using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Neo.SmartContract
{
    public class MTEsm : Framework.SmartContract
    {
        //---------------------------------------------------------------------------------------------
        // EVENTS
        
        [DisplayName("transaction")]
        public static event Action<byte[], byte[], BigInteger, BigInteger> Transfer;
        [DisplayName("transaction")]
        public static event Action<string, byte[], BigInteger, BigInteger> Retract;
        [DisplayName("membership")]
        public static event Action<byte[], string> Membership;
        [DisplayName("process")]
        public static event Action<string, string> Process;
        [DisplayName("ballot")]
        public static event Action<string, byte[], bool> Ballot;
        [DisplayName("offer")]
        public static event Action<string, byte[], BigInteger> Offer;
        [DisplayName("change")]
        public static event Action<string, object> Update;
        [DisplayName("invalid operation")]
        public static event Action<string> Exception;

        //---------------------------------------------------------------------------------------------
        // GLOBAL VARIABLES
        
        // The total number of referendum processes.
        private static BigInteger NumOfRef() => Storage.Get("numofref").AsBigInteger();
        
        // The total number of power plant (PP) units.
        private static BigInteger NumOfPP() => Storage.Get("numofpp").AsBigInteger();
        
        // The total number of members.
        private static BigInteger NumOfMemb() => Storage.Get("numofmemb").AsBigInteger();
        
        // The group total power supply, i.e., sum of PP's capacity.
        private static BigInteger TotalSupply() => Storage.Get("totalsupply").AsBigInteger();
        
        // The member's dataset settings.
        private static string[] profile => new string[] {"fullname", "utility"};
        private static string[] register => new string[] {"quota", "tokens"};
        private struct MemberData
        {
            public static StorageMap ID => Storage.CurrentContext.CreateMap(nameof(ID));
            public static StorageMap FullName => Storage.CurrentContext.CreateMap(nameof(FullName));
            public static StorageMap Utility => Storage.CurrentContext.CreateMap(nameof(Utility));
            public static StorageMap Quota => Storage.CurrentContext.CreateMap(nameof(Quota));
            public static StorageMap Tokens => Storage.CurrentContext.CreateMap(nameof(Tokens));
        }
        
        // The referendum's dataset settings.
        private struct RefData
        {
            public static StorageMap ID => Storage.CurrentContext.CreateMap(nameof(ID));
            public static StorageMap Proposal => Storage.CurrentContext.CreateMap(nameof(Proposal));
            public static StorageMap Notes => Storage.CurrentContext.CreateMap(nameof(Notes));
            public static StorageMap Cost => Storage.CurrentContext.CreateMap(nameof(Cost));
            public static StorageMap Address => Storage.CurrentContext.CreateMap(nameof(Address));
            public static StorageMap Time => Storage.CurrentContext.CreateMap(nameof(Time));
            public static StorageMap MoneyRaised => Storage.CurrentContext.CreateMap(nameof(MoneyRaised));
            public static StorageMap NumOfVotes => Storage.CurrentContext.CreateMap(nameof(NumOfVotes));
            public static StorageMap CountTrue => Storage.CurrentContext.CreateMap(nameof(CountTrue));
            public static StorageMap Outcome => Storage.CurrentContext.CreateMap(nameof(Outcome));
            public static StorageMap HasResult => Storage.CurrentContext.CreateMap(nameof(HasResult));
            public static StorageMap StartTime => Storage.CurrentContext.CreateMap(nameof(StartTime));
            public static StorageMap EndTime => Storage.CurrentContext.CreateMap(nameof(EndTime));
        }
        
        // The PP's dataset settings.
        private struct PPData
        {
            public static StorageMap ID => Storage.CurrentContext.CreateMap(nameof(ID));
            public static StorageMap Capacity => Storage.CurrentContext.CreateMap(nameof(Capacity));
            public static StorageMap Cost => Storage.CurrentContext.CreateMap(nameof(Cost));
            public static StorageMap Utility => Storage.CurrentContext.CreateMap(nameof(Utility));
            public static StorageMap TimeToMarket => Storage.CurrentContext.CreateMap(nameof(TimeToMarket));
            public static StorageMap NumOfFundMemb => Storage.CurrentContext.CreateMap(nameof(NumOfFundMemb));
            public static StorageMap HasStarted => Storage.CurrentContext.CreateMap(nameof(HasStarted));
        }
        
        // The ICO's dataset settings (for crowdfunding).
        private struct ICOData
        {
            public static StorageMap StartTime => Storage.CurrentContext.CreateMap(nameof(StartTime));
            public static StorageMap EndTime => Storage.CurrentContext.CreateMap(nameof(EndTime));
            public static StorageMap TotalAmount => Storage.CurrentContext.CreateMap(nameof(TotalAmount));
            public static StorageMap Contributions => Storage.CurrentContext.CreateMap(nameof(Contributions));
            public static StorageMap Success => Storage.CurrentContext.CreateMap(nameof(Success));
            public static StorageMap HasResult => Storage.CurrentContext.CreateMap(nameof(HasResult));
        
            public static StorageMap Bid => Storage.CurrentContext.CreateMap(nameof(Bid));
        }
        
        // The predefined periods to answer both a referendum and a crowdfunding, and to wait until a PP construction.
        private const uint timeFrameRef = 120;           // 30 days = 2592000
        private const uint timeFrameCrowd = 5184000;     // 60 days
        private const uint minTimeToMarket = 2592000;    // 30 days

        // The essential settings to support the process of a new PP crowdfunding.
        private const int  minOffer = 100;      // Brazilian Reais (R$)
        private const uint factor = 1000;       // 1kW == 1SEB
        
        // The token basic settings.
        private static string Name() => "Sharing Electricity in Brazil";
        private static string Symbol() => "SEB";

        // The power limits of the distributed generation category defined by Brazilian law (from 0MW to 5MW).
        private static int[] PowGenLimits() => new int[] {0, 5000000};
        
        // The time a given function is invoked.
        private static uint InvokedTime() => Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;

        // The trick to lock the admission operation process without a referendum.
        private static void OnlyOnce() => Storage.Put("firstcall", 1);
        
        // The trick to support the conversion from 'int' to 'string'.
        private static string[] Digits() => new string[10] {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9"};
        
        // The characters of the Base58 scheme.
        private const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        
        
        //---------------------------------------------------------------------------------------------
        // THE MAIN INTERFACE
        
        public static object Main ( byte[] address, string operation, params object[] args )
        {
            // General operation.
            if ( operation == "admission" )
            {
                if ( args.Length != 2 )
                    return Warning("Please provide the 2 arguments: your full name, and the power utility name.");
        
                if ( !Runtime.CheckWitness(address) )
                    return Warning("The admission can not be done on someone else's behalf.");
        
                if ( ( (string)GetMemb(address) ).Length != 0 )
                    return Warning("Thanks, you're already a member. We're glad to have you as part of the group!");
                
                if ( Storage.Get("firstcall").AsBigInteger() == 0 )
                {
                    // No admission process is required.
        
                    // Locks this 'if' statement.
                    OnlyOnce();
                    
                    // Defines the 'invoker/caller' as the first member.
                    Membership( address, "Welcome on board!" );
                    Member( address, (string)args[0], (string)args[1], 0, 0 );
                    return true;
                }
        
                return Admission( address,           // invoker/caller address
                                  (string)args[0],   // fullName
                                  (string)args[1] ); // utility
            }
            
            // Partially restricted operation.
            if ( operation == "summary" )
            {
                if ( args.Length < 1 )
                    return Warning("Provide at least a member address or a PP ID.");
                
                if ( ((string)GetMemb((byte[])args[0])).Length != 0 )
                {
                    // The args[0] is a member, i.e.,
                    // it has being requested information about a member.

                    if ( !Runtime.CheckWitness(address) )
                        return Warning("This request can not be done on someone else's behalf.");
                    
                    if ( ((string)GetMemb(address)).Length == 0 )
                        return Warning();
                }
                
                return Summary( (object)args[0],     // any ID
                                (string)args[1] );   // desired option
            }
        
            // Restricted operations.
            if ( ((string)GetMemb(address)).Length != 0 )
            {
                
                // Group operations.
                if ( operation == "vote" )
                {
                    if ( args.Length != 2 )
                        return Warning("Please provide the 2 arguments: the referendum ID, and your vote.");
        
                    if ( !Runtime.CheckWitness(address) )
                        return Warning("The vote can not be done on someone else's behalf.");
        
                    if ( isLock( (string)args[0]) )
                        return Warning("The ballot has ended.");
                    
                    return Vote( (string)args[0],   // referendum ID
                                 address,           // member address
                                 (bool)args[1] );   // vote answer
                }
        
                if ( operation == "bid" )
                {
                    if ( args.Length != 2 )
                        return Warning("Please provide the 2 arguments: the PP ID, and your bid.");
        
                    if ( !Runtime.CheckWitness(address) )
                        return Warning("The bid can not be done on someone else's behalf.");
        
                    if ( (((string)args[0])[0] != 'P') || (((string)args[0]).Length == 0) )
                        return Warning("Provide a valid PP ID.");
        
                    if ( (GetPP((string)args[0], "utility")) != (GetMemb(address, "utility")) )
                        return Warning("This member cannot profit from this power utility." );
        
                    if ( (int)args[1] <= minOffer )
                        return Warning(String.Concat("The minimum bid allowed is R$ ", Int2Str(minOffer)));
                    
                    if ( isLock( (string)args[0] ) )
                        return Warning("The crowdfunding has ended.");
        
                    return Bid( (string)args[0],        // PP ID
                                address,                // member address
                                (BigInteger)args[1] );  // bid value
                }
        
                if ( operation == "change" )
                {
                    if ( args.Length != 2 )
                        return Warning("Please provide 2 arguments only. The first one must be either the identification of the member (address) or the PP (ID). The second one must be an array. It can be either the options about the data that will be changed, or an empty array to request the deletion of something.");
                    
                    // To simplify the indexing.
                    var opt = (object[])args[1];
                    
                    // Should be a PP ID.
                    if ( IsValidId(args[0]) ) 
                    {
                        if ( ((string)GetPP((string)args[0], "utility")).Length == 0 )
                            return Warning("Provide a valid PP ID.");

                        if ( opt.Length != 1 )
                            return Warning("Only one option is required to update a PP subject. It can be a PP utility name, or a new bid value for a PP crowdfunding campaign.");
                        
                        // It should be a 'BigInteger'.
                        if ( IsValidNum(opt[0]) )
                        {
                            if ( isLock( (string)args[0] ) )
                                return Warning("The crowdfunding has ended.");

                            if ( !(Runtime.CheckWitness(address)) )
                                return Warning("Only the member can change its bid.");
                                
                            // Updates the option array to pass the 'address' together with the bid value.
                            int i = opt.Length;
                            object[] option = new object[i+1];
                            
                            while( i > 0 )
                            {
                                option[i] =  opt[i-1];
                                i--;
                            }
                            option[i] = address;
                            
                            return Change( (object)args[0], // PP ID
                                            option );       // array with desired values
                        }
                    }
                    
                    // Should be a member ID (address).
                    else
                    {
                        if ( ((string)GetMemb((byte[])args[0])).Length == 0 )
                            return Warning("Provide a valid member address.");

                        if ( (opt.Length != 2) & (opt.Length != 0) )
                            return Warning("Provide valid arguments to update/delete an address.");

                        if ( ( ((string)opt[0] == profile[0]) | ((string)opt[0] == profile[1]) ) & !(Runtime.CheckWitness(address)) )
                            return Warning("Only the member can change her/his profile data.");
                    }
                    
                    return Change( (object)args[0], // member address or PP ID
                                   opt );           // array with desired values
                }
        
                if ( operation == "power up" )
                {
                    if ( args.Length != 4 )
                        return Warning("Please provide the 4 arguments: the PP capacity, the cost to build it up, the power utility name in which the PP will be connected to, and the period to wait until the new PP gets ready to operate.");
        
                    if ( ((int)args[3] == 0) || ((int)args[3] < minTimeToMarket) )
                        return Warning("The time to market must be a factual period.");
        
                    return PowerUp( (int)args[0],       // capacity [MW]
                                    (int)args[1],       // cost [R$]
                                    (string)args[2],    // power utility name
                                    (uint)args[3] );    // time to market
                }

                if ( operation == "trade" )
                {
                    if ( args.Length != 3 )
                        return Warning("Please provide the 3 arguments: the address of who you are transacting to, the quota value, and the amount of tokens.");
        
                    if ( !Runtime.CheckWitness(address) )
                        return Warning("Only the owner of an account can exchange her/his asset.");
                    
                    if ( ((string)GetMemb((byte[])args[0])).Length == 0 )
                        return Warning("The address you are transacting to must be a member too.");
        
                    if ( (GetMemb(address, "utility")) != (GetMemb((byte[])args[0], "utility")) )
                        return Warning("Both members must belong to the same power utility coverage area.");
        
                    if ( ((int)args[1] <= 0) & ((int)args[2] <= 0) )
                        return Warning("You're doing it wrong. To donate energy let ONLY the 3rd argument empty. Otherwise, to donate tokens let ONLY the 2nd argument empty.");
                    
                    return Trade( address,               // from address
                                  (byte[])args[0],       // to address
                                  (BigInteger)args[1],   // quota exchange
                                  (BigInteger)args[2] ); // token price
                }
                
                // Administrative operations.
                if ( operation == "admission result" )
                {
                    if ( args.Length != 1 )
                        return Warning("Please provide only the admission process ID.");
                    
                    if ( isLock( (string)args[0], "inv" ) )
                        return Warning("There isn't a result yet.");
                    
                    return AdmissionResult( (string)args[0] ); // Referendum ID
                }
                
                if ( operation == "change result" )
                {
                    if ( args.Length != 1 )
                        return Warning("Please provide only the change process ID.");
                    
                    if ( isLock( (string)args[0], "inv" ) )
                        return Warning("There isn't a result yet.");
                    
                    return ChangeResult( (string)args[0] ); // Referendum ID
                }
                
                if ( operation == "power up result" )
                {
                    if ( args.Length == 0 )
                        return Warning("Please provide at least the new PP process ID.");
                        
                    if ( args.Length > 2 )
                        return Warning("Please provide at most the new PP process ID, and the PP ID itself if any.");
                    
                    return PowerUpResult( (string)args[0],     // Referendum ID
                                          (string)args[1] );   // PP ID
                }
        
                if ( operation == "list of power plants" )
                {
                    if ( args.Length != 0 )
                        return Warning("This function does not need attributes.");
                    
                    return ListOfPPs();
                }
        
                if ( operation == "list of members" )
                {
                    if ( args.Length != 0 )
                        return Warning("This function does not need attributes.");
                    
                    return ListOfMembers();
                }
            }
        
            return Warning("No operation found. Have you written it right?");
        }
        
        
        //---------------------------------------------------------------------------------------------
        // GROUP FUNCTIONS - The restrictions are made on the 'Main'.
        
        // To request to join the group.
        private static string Admission( byte[] address, string fullName, string utility )
        {
            string rID = Ref( fullName, utility, address );
            Membership( address, "Request for admission." );
            return rID;
        }
        
        // To get information about something.
        private static object Summary( object id, string opt = null )
        {
            // If 'id' is a 'byte[]' == member.
            if ( ((byte[])id).Length == 20 )
            {
                var address = (byte[])id;
                
                if ( (opt == "") || (opt == "detailed") )
                {
                    object[] brief = new object[] { GetMemb(address), GetMemb(address,"utility"), GetMemb(address,"quota"), GetMemb(address,"tokens") };

                    if ( opt == "detailed" )
                    {
                        ShowContributedValues( address, ListOfPPs() );
                    }
                    return brief;
                }
                return GetMemb(address, opt);
            }

            // If 'id' is a 'string' with prefix 'P' == power plant.
            else if ( ((string)id)[0] == 'P' )
            {
                var ppID = (string)id;
                
                // The PP's crowdfunding had succeed and the PP is operating.
                if ( (bool)GetPP(ppID) )
                {
                    if ( (opt == "") || (opt == "detailed") )
                    {
                        object[] brief = new object[] { GetPP(ppID,"capacity"), GetPP(ppID,"cost"), GetPP(ppID,"utility"), GetPP(ppID,"numoffundmemb") };
            
                        if ( opt == "detailed" )
                        {
                            ShowContributedValues( ppID, ListOfMembers() );
                        }
                        return brief;
                    }
                    return GetPP(ppID, opt);
                }
                
                // The PP's crowdfunding may be succeed or not, and the PP is definitely not operating.
                else
                {
                    if ( (opt == "") || (opt == "detailed") )
                    {
                        object[] brief = new object[] { GetCrowd(ppID,"starttime"), GetCrowd(ppID,"endtime"), GetCrowd(ppID,"totalamount"), GetCrowd(ppID,"contributions"), GetCrowd(ppID,"success") };

                        if ( opt == "detailed" )
                        {
                            foreach ( byte[] member in ListOfMembers() )
                            {
                                BigInteger bid = GetBid(ppID, member);
                                
                                if ( bid != 0 )
                                {
                                    Runtime.Notify( new object[] { member, bid } );
                                }
                            }
                        }
                        return brief;
                    }
                    return GetCrowd(ppID, opt);
                }
            }

            // If 'id' is a 'string' with prefix 'R' == referendum process.
            else if ( ((string)id)[0] == 'R' )
            {
                var rID = (string)id;
                
                if ( opt == "" )
                {
                    return new object[] { GetRef(rID,"proposal"), GetRef(rID,"notes"), GetRef(rID,"cost"), GetRef(rID,"outcome") };
                }
                return GetRef(rID, opt);
            }

            // Wrap-up the group information.
            else
            {
                return new object[] { PowGenLimits()[0], PowGenLimits()[1], NumOfPP(), NumOfMemb(), Name(), Symbol(), TotalSupply() };
            }
        }
        
        // To vote in a given process.
        private static bool Vote( string rID, byte[] member, bool answer )
        {
            // Increases the number of votes.
            BigInteger temp = (BigInteger)GetRef(rID,"numofvotes");
            UpRef(rID, "numofvotes", temp+1);

            if ( answer )
            {
                // Increases the number of "trues".
                temp = (BigInteger)GetRef(rID,"counttrue");
                UpRef(rID, "counttrue", temp+1);
            }

            // Publishes the vote.
            Ballot(rID, member, answer);
            return true;
        }
        
        // To make a bid in a new PP crowdfunding process.
        private static bool Bid( string ppID, byte[] member, BigInteger bid )
        {
            BigInteger target = (BigInteger)GetPP(ppID, "cost");
            BigInteger funds = (BigInteger)GetCrowd(ppID, "totalamount");
            
            if ( bid > (target - funds) )
                return Warning( String.Concat(String.Concat("You offered more than the amount available (R$ ", Int2Str((int)(target - funds)) ), ",00). Bid again!" ));

            // WARNING!
            // All the following steps are part of a crowdfunding process.
            // Although the PP already has a register (i.e. a PP ID),
            // it does not have started to operate (PPData.HasStarted = false).
            
            // Increases the value gathered so far.
            UpCrowd(ppID, "totalamount", funds + bid);
            
            // Increases the number of contributions.
            BigInteger temp = (BigInteger)GetCrowd(ppID, "contributions");
            UpCrowd(ppID, "contributions", temp+1);
            
            // Tracks bid by member for each ICO process.
            UpBid(ppID, member, bid);
            Offer(ppID, member, bid);
            
            return true;
            
            // If the whole fund process succeed, the money bid must be converted to percentage (bid/cost),
            // so it will be possible to define the quota and the SEB a member has to gain.
            // This is made on PowerUpResult(...).
        }
        
        // To update a member or a PP dataset on the ledger.
        private static object Change( object id, params object[] opts )
        {
            // A referendum must start in case the change needs group's consensus.
            string rID;

            // If 'id' is a 'byte[]' == member.
            if ( ((byte[])id).Length == 20 )
            {
                if ( opts.Length != 0 )
                {
                    // Only the member can change its own personal data.
                    // To UPDATE, the params must be ['profile option', 'value'].
                    if ( !IsValidNum(opts[1]) )
                    {
                        UpMemb((byte[])id, (string)opts[0], (string)opts[1]);
                        Update("Profile data.", id);
                        return true;
                    }
                    
                    // Any member can request the change of registration data of other member.
                    // To UPDATE, the params must be ['register option', 'value'].
                    rID = Ref( "Change register_", (string)opts[0], (byte[])id, (int)opts[1] );
                    Process( rID, "Request the change of a member's registration data." );
                    return rID;
                }

                // else
                // Any member can request to delete another member.
                rID = Ref( "Delete member_", null, (byte[])id );
                Process(rID, "Request to dismiss a member.");
                return rID;
            }
            
            // Otherwise, the 'id' is a 'string' with prefix 'P' == power plant.

            // Only the member can change its own bid.
            // To UPDATE, the params must be ['address', 'new bid value'].
            if ( opts.Length == 2 )
            {
                UpBid((string)id, (byte[])opts[0], (BigInteger)opts[1]);
                Update("Bid.", id);
                return true;
            }
            
            // Any member can request the change of the 'utility' a PP belongs to.
            // To UPDATE, the params must be ['new utility name'].
            if ( opts.Length == 1 )
            {
                rID = Ref( "Change utility_", (string)opts[0],  ((string)id).AsByteArray() );
                Process( rID, "Request the change of a PP's utility name." );
                return rID;
            }

            // Any member can request to DELETE a PP.
            // The 'opts.Length' is empty.
            rID = Ref("Delete PP_", null, ((string)id).AsByteArray());
            Process(rID, "Request to delete a PP.");
            return rID;
        }
        
        // To integrate a new PP on the group power generation.
        private static string PowerUp( int capacity, int cost, string utility, uint timeToMarket )
        {
            string rID = Ref( Int2Str(capacity), utility, "".AsByteArray(), cost, timeToMarket );
            Process( rID, "Request to add a new PP." );
            return rID;
        }
        
        // To allow the transfer of shares/tokens from someone to someone else (transactive energy indeed).
        // The 'fromAddress' will exchange an amount of shares with 'toAddress' by a defined token price,
        // i.e., while 'fromAddress' sends shares to 'toAddress', the 'toAddress' sends tokens to 'fromAddress'.
        private static bool Trade( byte[] fromAddress, byte[] toAddress, BigInteger exchange, BigInteger price )
        {
            int n = 2;
            BigInteger[] toWallet = new BigInteger[n];
            BigInteger[] fromWallet = new BigInteger[n];
            
            for ( int r = 0; r < n; r++ )
            {
                // Remember: register = {"quota", "tokens"}.

                fromWallet[r] = (BigInteger)GetMemb( fromAddress, register[r] );
                toWallet[r] = (BigInteger)GetMemb( toAddress, register[r] );
            }
            
            if ( ( fromWallet[0] < exchange ) || ( toWallet[1] < price ) ) return false;
            
            UpMemb(fromAddress, register[0], fromWallet[0] - exchange);
            UpMemb(toAddress, register[0], toWallet[0] + exchange);
            
            UpMemb(toAddress, register[1], toWallet[1] - price);
            UpMemb(fromAddress, register[1], fromWallet[1] + price);
            
            Transfer(fromAddress, toAddress, exchange, price);
            return true;
        }
        
        
        //---------------------------------------------------------------------------------------------
        // ADMINISTRATIVE FUNCTIONS
        // After a period of 'timeFrameRef' days, a member should invoke the below functions to state
        // the referendum process. An off-chain operation should handle this waiting time.

        private static bool AdmissionResult( string rID )
        {
            // Calculates the result.
            CalcResult(rID);
            
            // Retrives the address from private storage.
            byte[] address = (byte[])GetRef(rID, "address");

            if ( Str2Bool( (string)GetRef(rID, "outcome") ) )
            {
                // Retrives the member data from private storage.
                string fullName = (string)GetRef(rID, "proposal");
                string utility = (string)GetRef(rID, "notes");

                // Adds a new member after the group approval.
                Member( address, fullName, utility, 0, 0 );
                Membership( address, "Welcome on board!" );
                return true;
            }

            // Otherwise, leave the user out of the group.
            Membership( address, "Not approved yet." );
            DelMemb( address );
            return false;
        }

        private static bool ChangeResult( string rID )
        {
            // Calculates the result.
            CalcResult(rID);
            
            if ( Str2Bool( (string)GetRef(rID, "outcome") ) )
            {
                Process(rID, "Approved.");

                // Identifies the proposal and does the respective operation.
                string proposal = (string)GetRef(rID, "proposal");

                byte[] key;

                if ( proposal == "Change register_" )
                {
                    key = (byte[])GetRef(rID, "address");
                    UpMemb(key, (string)GetRef(rID, "notes"), (BigInteger)GetRef(rID, "cost"));
                    Update("Registration data.", key);
                }
                            
                if ( proposal == "Delete member_" )
                {
                    key = (byte[])GetRef(rID, "address");
                    BigInteger portion = (BigInteger)GetMemb(key, "quota");
                    BigInteger give_out = portion/(NumOfMemb() - 1);
                    
                    foreach ( byte[] member in ListOfMembers() )
                    {
                        // In an infinitesimal period of time the group will be disbalanced
                        // until the related member be completely deleted.
                        // There is no side effect on power distribution, and
                        // it is better than iterate through each member.
                        
                        Distribute(member, give_out, 0);
                    }
            
                    DelMemb(key);
                    Membership(key, "Goodbye.");
                }
                
                if ( proposal == "Change utility_" )
                {
                    UpPP(rID, "utility", (string)GetRef(rID, "notes"));
                    Update("Belonging of.", rID);
                }
                
                if ( proposal == "Delete PP_" )
                {
                    DelPP(rID);
                    Update("Deletion of.", rID);
                }

                return true;
            }

            Process(rID, "Denied.");
            return false;
        }

        private static object PowerUpResult( string rID, string ppID = null )
        {
            // STEP 1 - Analyzes the referendum about the request for a new PP.
            if ( ppID == null )
            {
                if ( isLock(rID, "inv") )
                    return Warning("There isn't a result about the new PP request yet.");
                
                // After the 'timeFrameRef' waiting period...

                // Evaluates the referendum result only once.
                if ( (BigInteger)GetRef(rID) == 0 )
                {
                    // Updates the result.
                    CalcResult(rID);
                    
                    if ( Str2Bool( (string)GetRef(rID, "outcome") ) )
                    {
                        // Referendum has succeeded. It's time to register a new PP.
                        
                        // Gets the terms from the begining of the process.
                        string capacity = (string)GetRef(rID, "proposal");
                        BigInteger cost = (BigInteger)GetRef(rID, "cost");
                        string utility = (string)GetRef(rID, "notes");
                        uint timeToMarket = (uint)GetRef(rID, "time");
                        
                        // Generates the PP ID.
                        string PPid = PP(capacity, cost, utility, timeToMarket);
                        
                        // Starts to raise money for it.
                        CrowdFunding(PPid);
                        Process(PPid, "Shut up and give me money!");
                        return PPid;
                    }
                    
                    // Otherwise, the referendum of the PP request (Ref ID) continues registered
                    // in the group space, however it does not have a register (PP ID).
                    Process(rID, "This PP was not approved yet. Let's wait a bit more.");
                    return false;
                }
                
                return "This process step is completed.";
            }
            
            // STEP 2 - Analyzes the crowdfunding of the new PP approved.
            if ( isLock(ppID, "inv") )
                return Warning("There isn't a result about the new PP crowdfunding yet.");
            
            // After the 'timeFrameCrowd' waiting period...

            // Keeps the value for the following operations handy.
            BigInteger target = (BigInteger)GetPP(ppID, "cost");

            // Evaluates the crowdfunding result only once.
            if ( (BigInteger)GetCrowd(ppID) == 0 )
            {                
                // Updates the result.
                UpCrowd(ppID, "hasresult", 1);

                // Gets the value from the crowdfunding process.
                BigInteger funding = (BigInteger)GetCrowd(ppID, "totalamount");

                // Evaluates if the building of the new PP starts or not.
                if ( funding == target )
                {
                    // Crowdfunding has succeeded.
                    UpCrowd(ppID, true);
                    
                    // Updates the number of investors.
                    UpPP(ppID, "numOfFundMemb", ListOfFunders(ppID).Length);
                    
                    Process(ppID, "New power plant on the way.");
                    return true;
                }
                
                // Otherwise, the "success" remains as 'false'.
                foreach ( byte[] funder in ListOfFunders(ppID) )
                {
                    Cancel(ppID, funder);
                }
                
                Process(ppID, "Fundraising has failed.");
                return false;
            }
            
            // STEP 3 - Analyzes the PP operation status.

            // Calculates the date the new PP is planned to start to operate,
            // that can be always updated until the deadline.
            
            // operationDate = ICO_endTime + PP_timeToMarket
            uint operationDate = (uint)GetCrowd(ppID, "endtime") + (uint)GetPP(ppID, "timetomarket");
            
            if ( InvokedTime() <= operationDate )
                return Warning("The new PP is not ready to operate yet.");
            
            // After waiting for the time to market...

            // Evaluates the construction only once.
            if ( (BigInteger)GetPP(ppID) == 0 )
            {
                // When the PP is ready to operate, it's time to distribute tokens and shares.

                // Increases the total power supply of the group.
                BigInteger capOfPP = (BigInteger)GetPP(ppID, "capacity");       // [MW]
                BigInteger capOfGroup = TotalSupply() + capOfPP;                // [MW]
                Storage.Put("totalsupply", capOfGroup);
            
                // Identifies how much the new PP takes part on the group total power supply.
                BigInteger sharesOfPP = capOfPP/capOfGroup;                     // [pu]

                foreach ( byte[] funder in ListOfFunders(ppID) )
                {
                    // Gets the member contribution.
                    BigInteger grant = GetBid(ppID, funder);                    // [R$]

                    // Identifies the member participation rate.
                    BigInteger rate = grant/target; // [pu]
                    
                    // Defines how much of crypto-currency a member acquires from the new PP's capacity.
                    BigInteger tokens = (rate * capOfPP)/factor;                // [MW/1000 = kW == SEB]
                    
                    // Defines how much of energy a member is entitled over the total power supply.
                    BigInteger quota = rate * sharesOfPP * capOfGroup;          // [MW]
            
                    // Updates the member register data.
                    Distribute(funder, quota, tokens);
                }
            
                // Updates the result.
                UpPP(ppID, "hasstarted", 1);

                Process(ppID, "A new power plant is now operating.");
                return true;
            }
            
            return "There is nothing more to be done.";
        }

        // To return the IDs of each PP.
        private static byte[][] ListOfPPs()
        {
            byte[][] ppIDs = new byte[ (int)NumOfPP() ][];
            
            for ( int num = 0; num < NumOfPP(); num++ )
            {
                ppIDs[num] = PPData.ID.Get( Int2Str(num+1) );
            }

            return ppIDs;
        }

        // To return the address of each member.
        private static byte[][] ListOfMembers()
        {
            byte[][] addresses = new byte[ (int)NumOfMemb() ][];
            
            for ( int num = 0; num < NumOfMemb(); num++ )
            {
                addresses[num] = MemberData.ID.Get( Int2Str(num+1) );
            }
            return addresses;
        }
        
        // To return a list of members that have financed a given PP.
        private static byte[][] ListOfFunders( string ppID )
        {
            byte[][] funders = new byte[ (int)GetCrowd(ppID, "contributions") ][];

            BigInteger bid;
            int num = 0;
            foreach ( byte[] member in ListOfMembers() )
            {
                bid = GetBid( ppID, member );
                
                if ( bid != 0 )
                {
                    funders[num] = member;
                    num++;
                }
            }

            return funders;
        }
        
        
        //---------------------------------------------------------------------------------------------
        // SYSTEM FUNCTIONS
        
        // A new PP will only distribute tokens and shares after a
        // crowdfunding process succeed and the PP starts to operate.
        // All the exceptions were handle during the crowdfunding.
        // Now, it only needs to distribute the assets.
        private static void Distribute( byte[] toAddress, BigInteger quota, BigInteger tokens )
        {
            BigInteger[] pastWallet = new BigInteger[ register.Length ];
            int num = 0;
            
            // Remember: register = {"quota", "tokens"}.
            foreach ( string data in register )
            {
                pastWallet[num] = ( (BigInteger)GetMemb(toAddress, data) );
                num++;
            }
            
            UpMemb(toAddress, register[0], pastWallet[0] + quota);
            UpMemb(toAddress, register[1], pastWallet[1] + tokens);
            Transfer(null, toAddress, quota, tokens);
        }
        
        // To create a custom ID of a process based on its particular specifications.
        private static string ID( string prefix, bool unique, params string[] args )
        {
            // Assuming that all operations are little-endian.
            
            // STEP 1 - Creates the hash.
            string data = null;

            if ( unique ) data = Int2Str((int)InvokedTime());

            foreach ( string a in args )
            {
                data = String.Concat(data,a);
            }

            byte[] scriptHash = Hash160( data.AsByteArray() );          // length = 20 bytes
            
            // STEP 2 - Enlarges the array to get the desired BigInteger's numbers range.
            byte[] temp = scriptHash.Take(1);
            scriptHash = scriptHash.Concat(temp);                       // length = 21 bytes
            
            // STEP 3 - Adds the prefix.
            byte[] preID = scriptHash.Concat( prefix.AsByteArray() );   // length = 22 bytes
            
            // STEP 3 - Converts to Base58.
            return Encode58( preID );
        }
        
        // To properly store a boolean variable.
        private static string Bool2Str( bool val )
        {
            if ( val ) return "1";
            return "0";
        }
        
        // To properly read a boolean from storage.
        private static bool Str2Bool( string val )
        {
            if ( val == "1" ) return true;
            return false;
        }
        
        // To affordably convert an integer to a string.
        private static string Int2Str( int num, string s = null )
        {
            if ( num == 0 ) return s;

            int quotient = num / 10;
            int remainder = num % 10;
            
            string trick = Digits()[ remainder ];
                
            return Int2Str(quotient, String.Concat(trick, s) );
        }
        
        // The Base58 enconding scheme.
        private static string Encode58( byte[] preID )
        {
            // Restricts to positive values.
            byte[] data = preID.Concat("\x00".AsByteArray());   // length = 23 bytes
            
            // Converts 'byte[]' to 'BigInteger' and then to 'int'.
            int input = (int)data.ToBigInteger();
            
            // Defines the variables for the encode.
            int[] result = new int[40]; // Big value to avoid constraints.
            int basis = 58;
            int pos = 0;
            int quotient = basis+1;

            // Starts the encode with the Base58 indexes.
            while (quotient > basis)
            {
                quotient = input / basis;
                result[pos] = input % basis;
                input = quotient;
                pos++;
            }
            result[pos] = input;
            
            // Converts the array of indexes to 'string'.
            string b58 = null;
            for ( int k=pos; k >= 0; k-- )
            {
                b58 += Alphabet[ result[k] ];
            }
            
            return b58;
        }
        
        // To evaluate if an object is a 'string' that may represent both a PP ID or a Ref ID.
        private static bool IsValidId( object id )
        {
            // Checks if the Referendum exists.
            if ( ((string)GetRef( (string)id, "proposal" )).Length != 0 ) return true;
        
            // Checks if the PP exists.
            if ( ((string)GetPP( (string)id, "capacity" )).Length != 0 ) return true;
        
            return false;
        }

        // To evaluate if an object can be converted to 'BigInteger'.
        private static bool IsValidNum( object test )
        {
            string temp = (string)test;
            
            foreach ( char t in temp )
            {
                foreach ( string d in Digits() )
                {
                    if ( t.ToString() == d ) return false;
                }
            }
            return true;
        }

        // To filter the relationships between members and PPs.
        private static void ShowContributedValues( object lookForID, object[] listOfIDs )
        {
            BigInteger bid;

            // Displays all the members and their contributions to a given PP crowdfunding.
            if ( IsValidId(lookForID) )
            {
                // The 'lookForID' is a PP ID.

                // Gets members' bid by a PP funding process.
                foreach ( byte[] member in listOfIDs )
                {
                    bid = GetBid((string)lookForID, member);
                    
                    if ( bid != 0 )
                    {
                        Runtime.Notify( new object[] { member, bid } );
                    }
                }
            }
            
            // Displays all the PPs and its funds from a specific member.
            else
            {
                // The 'lookForID' is a member address.

                // Gets PPs by a member investments.
                foreach ( string ppID in listOfIDs )
                {
                    bid = GetBid(ppID, (byte[])lookForID);
                    
                    if ( bid != 0 )
                    {
                        Runtime.Notify( new object[] { ppID, bid } );
                    }
                }
            }
        }
        
        // To calculate the referendum result only once.
        private static void CalcResult( string rID )
        {
            if ( !Str2Bool( (string)GetRef(rID) ) )
            {
                BigInteger totalOfVotes = (BigInteger)GetRef(rID, "numofvotes");
                BigInteger totalOfTrues = (BigInteger)GetRef(rID, "counttrue");
                    
                if ( totalOfTrues > (totalOfVotes / 2) )
                {
                    // Referendum has succeeded.
                    UpRef(rID, true);
                }                
                // Otherwise, the "outcome" remains as 'false'.

                // Certifies the calculation happens only once.
                UpRef(rID, "hasresult", 1);
            }
        }
        
        // Actually, it restricts a given operation to happen based on a timestamp.
        // However, the monitoring of the time happens off-chain.
        // For operations 'vote', 'bid', and 'change', no one is allowed to continue
        // the process after the 'endtime'.
        // On the other hand, the operations 'admission result', 'change result', and
        // 'power up result' stop the process before a given time frame. Once the time
        // stated is reached, any member can then resume the process.
        private static bool isLock( string id, string logic = null )
        {
            uint endTime;
            
            if ( id[0] == 'R' )
            {
                endTime = (uint)GetRef(id, "endtime");
            }
            else
            {
                // id[0] == 'P'
                endTime = (uint)GetCrowd(id, "endtime");
            }
            
            if ( logic == "inv" )
            {
                if ( InvokedTime() <= endTime ) return true;
                return false;
            }
            else
            {
                if ( InvokedTime() <= endTime ) return false;
                return true;
            }
        }
        
        // The restrictive message to show up.
        private static bool Warning(string msg = "Only members can access this information. Join us!")
        {
            Exception(msg);
            return false;
        }


        //---------------------------------------------------------------------------------------------
        // METHODS FOR MEMBERS
        // --> create
        private static void Member( byte[] address, string fullName, string utility, BigInteger quota, BigInteger tokens )
        {
            MemberData.FullName.Put(address, fullName);
            MemberData.Utility.Put(address, utility);
            MemberData.Quota.Put(address, quota);
            MemberData.Tokens.Put(address, tokens);

            // Increases the total number of members.
            BigInteger temp = NumOfMemb()+1;
            Storage.Put("numofmemb", temp);
            
            // Stores the address of each member.
            MemberData.ID.Put( Int2Str((int)temp), address );
        }
        
        // --> read
        private static object GetMemb( byte[] address, string opt = "fullname" )
        {
            if ( opt == "utility" ) return MemberData.Utility.Get(address);
            else if ( opt == "quota" ) return MemberData.Quota.Get(address);
            else if ( opt == "tokens" ) return MemberData.Tokens.Get(address);
            else return MemberData.FullName.Get(address);
        }
        
        // --> update
        // Detailed restrictions to update 'profile' or 'register' data are set
        // on the function 'Change'. Here this feature is handled by polymorphism.
        private static bool UpMemb( byte[] address, string opt, string val )
        {
            // Doesn't invoke Put if value is unchanged.
            string orig = (string)GetMemb(address, opt);
            if ( orig == val ) return true;
             
            // Use Delete rather than Put if the new value is empty.
            if ( val.Length == 0 )
            {
                DelMemb(address, opt);
                return true;
            }
            
            // else
            if ( opt == "fullname" ) MemberData.FullName.Put(address, val);
            if ( opt == "utility" ) MemberData.Utility.Put(address, val);

            return true;
        }

        private static bool UpMemb( byte[] address, string opt, BigInteger val )
        {
            // Doesn't invoke Put if value is unchanged.
            BigInteger orig = (BigInteger)GetMemb(address, opt);
            if ( orig == val ) return true;
             
            // Use Delete rather than Put if the new value is zero.
            if ( val == 0 )
            {
                DelMemb(address, opt);
                return true;
            }
            
            // else
            if ( opt == "quota" ) MemberData.Quota.Put(address, val);
            if ( opt == "tokens" ) MemberData.Tokens.Put(address, val);

            return true;
        }
        
        // --> delete
        private static void DelMemb( byte[] address, string opt = "" )
        {
            // To support an economic action for the update method.
            if ( opt == "fullname" ) MemberData.FullName.Delete(address);
            else if ( opt == "utility" ) MemberData.Utility.Delete(address);
            else if ( opt == "quota" ) MemberData.Quota.Delete(address);
            else if ( opt == "tokens" ) MemberData.Tokens.Delete(address);
            
            // The member exits the group database (opt == "").
            else
            {
                foreach ( string option in new string[]{"fullname", "utility", "quota", "tokens"} )
                {
                    DelMemb(address, option);
                }
                
                // Looks for the member 'key' (that may vary during the life cycle of the group).
                for ( int num = 1; num < NumOfMemb()+1; num++ )
                {
                    var index = Int2Str(num);
                    
                    if ( address == MemberData.ID.Get(index) )
                    {
                        // Wipes off the address of the member.
                        MemberData.ID.Delete(index);
                        
                        // Updates the following indexes.
                        while (num <= NumOfMemb())
                        {
                            num++;
                            var newIndexSameAddress = MemberData.ID.Get( Int2Str(num) );
                            MemberData.ID.Put( Int2Str(num-1), newIndexSameAddress );
                        }
                        
                        // Ends the for loop.
                        break;
                    }
                }

                // Decreases the total number of members.
                BigInteger temp = NumOfMemb() - 1;
                Storage.Put("numofmemb", temp);
            }
        }
        
        
        //---------------------------------------------------------------------------------------------
        // METHODS FOR POWER PLANTS
        // --> create
        private static string PP( string capacity, BigInteger cost, string utility, uint timeToMarket )
        {
            // Creates the unique identifier.
            string id = ID( "\x53", true, new string[] {capacity, Int2Str((int)cost), utility, Int2Str((int)timeToMarket)} );

            // Stores the practical values.
            PPData.Capacity.Put(id, capacity);
            PPData.Cost.Put(id, cost);
            PPData.Utility.Put(id, utility);
            PPData.TimeToMarket.Put(id, timeToMarket);

            // Just states the other values since it is expensive to store null values.
            // PPData.NumOfFundMemb.Put(id, 0);
            // PPData.HasStarted.Put(id, 0);

            // Increases the total number of PP units.
            BigInteger temp = NumOfPP()+1;
            Storage.Put("numofpp", temp);
            
            // Stores the ID of each PP.
            PPData.ID.Put( Int2Str((int)temp), id );

            // Notifies about the creation of the PP ID.
            Process(id, "New PP registered.");
            return id;
        }
        
        // --> read
        private static object GetPP( string ppID, string opt = "hasstarted" )
        {
            if ( opt == "capacity" ) return PPData.Capacity.Get(ppID);
            else if ( opt == "cost" ) return PPData.Cost.Get(ppID);
            else if ( opt == "utility" ) return PPData.Utility.Get(ppID);
            else if ( opt == "timetomarket" ) return PPData.TimeToMarket.Get(ppID);
            else if ( opt == "numoffundmemb" ) return PPData.NumOfFundMemb.Get(ppID);
            else return PPData.HasStarted.Get(ppID);
        }
        
        // --> update
        // The 'utility', the 'hasstarted', and the 'timetomarket' are the only options that can be changed.
        // However, the 'utility' can be changed anytime, the 'hasstarted' can be changed only once, while
        // the 'timetomarket' is restricted by its deadline of start operation date.
        // To update the other options, delete the current PP and create a new one.
        private static void UpPP( string ppID, string opt, object val )
        {
            if ( opt == "utility" )
            {
                // Doesn't invoke Put if value is unchanged.
                string orig = (string)GetPP(ppID, "utility");
                if ( orig == (string)val ) return;
                
                // Does nothing if the new value is empty.
                if ( ((string)val).Length == 0 ) return;
                
                // else
                PPData.Utility.Put(ppID, (string)val);
                // WARNING: Logic constraints!
                // When the PP utility name changes, it should update each member utility name as well.
                // However, only the member her/himself can change this information.
                // Therefore, 'utility' of both member's and PP's dataset must pointer to a common database.
                // THIS WAS NOT IMPLEMENTED!
            }
            
            if ( opt == "hasstarted" )
            {
                // Doesn't invoke Put if value is unchanged.
                BigInteger orig = (BigInteger)GetPP(ppID, "hasstarted");
                if ( orig == (BigInteger)val ) return;
                
                // Does nothing if the new value is empty.
                if ( (BigInteger)val == 0 ) return;
                
                // else
                PPData.HasStarted.Put(ppID, (BigInteger)val);
            }
            
            if ( opt == "timetomarket" )
            {
                // Doesn't invoke Put if value is unchanged.
                uint orig = (uint)GetPP(ppID, "timetomarket");
                if ( orig == (uint)val ) return;
                
                // Does nothing if the new value is empty.
                if ( (uint)val == 0 ) return;

                // Does nothing if the deadline has passed by.
                uint deadline = (uint)GetCrowd(ppID, "endtime") + (uint)GetPP(ppID, "timetomarket");
                
                if ( InvokedTime() > deadline )
                {
                    Warning("The time has passed by. You can no longer postpone it.");
                    return;
                }
                
                // else
                PPData.TimeToMarket.Put(ppID, (uint)val);
            }
        }

        // --> delete
        private static void DelPP( string ppID )
        {
            PPData.Capacity.Delete(ppID);
            PPData.Cost.Delete(ppID);
            PPData.Utility.Delete(ppID);
            PPData.TimeToMarket.Delete(ppID);
            if ( (BigInteger)GetPP(ppID, "hasstarted") != 0 ) PPData.HasStarted.Delete(ppID);
            if ( (BigInteger)GetPP(ppID, "numoffundmemb") != 0 ) PPData.NumOfFundMemb.Delete(ppID);
            
            // Looks for the PP 'key' (that may vary during the life cycle of the group).
            for ( int num = 1; num < NumOfPP()+1; num++ )
            {
                var index = Int2Str(num);

                if ( ppID == PPData.ID.Get(index).AsString() )
                {
                    // Wipes off the ID of the PP.
                    PPData.ID.Delete(index);
                    
                    // Updates the following indexes.
                    while (num <= NumOfMemb())
                    {
                        num++;
                        var newIndexSameId = PPData.ID.Get( Int2Str(num) );
                        PPData.ID.Put( Int2Str(num-1), newIndexSameId );
                    }

                    // Ends the for loop.
                    break;
                }
            }

            // Decreases the total power supply of power plants.
            BigInteger temp = TotalSupply() - (BigInteger)GetPP(ppID, "capacity");
            Storage.Put("totalsupply", temp);

            // Decreases the total number of power plant units.
            temp = NumOfPP() - 1;
            Storage.Put("numofpp", temp);
        }
        
        
        //---------------------------------------------------------------------------------------------
        // METHODS FOR REFERENDUMS
        // --> create
        private static string Ref( string proposal, string notes, byte[] address, int cost = 0, uint time = 0 )
        {
            string id = ID( "\x5A", true, new string[] {proposal, notes, Int2Str(cost)} );

            // Stores the practical values.
            RefData.Proposal.Put(id, proposal);
            RefData.Notes.Put(id, notes);
            RefData.Outcome.Put(id, Bool2Str(false));
            RefData.StartTime.Put(id, InvokedTime());
            RefData.EndTime.Put(id, InvokedTime() + timeFrameRef);

            // Evaluates the values before stores them since it is expensive to store null values.
            if ( address.Length != 0 ) RefData.Address.Put(id, address);
            if ( cost != 0 ) RefData.Cost.Put(id, cost);
            if ( time != 0 ) RefData.Time.Put(id, time);
            
            // Just states the other values since it is expensive to store null values.
            // RefData.MoneyRaised.Put(id, 0);
            // RefData.NumOfVotes.Put(id, 0);
            // RefData.CountTrue.Put(id, 0);
            // RefData.HasResult.Put(id, 0);
            
            // Increases the total number of referendum processes.
            BigInteger temp = NumOfRef()+1;
            Storage.Put("numofref", temp);
            
            // Stores the ID of each referendum.
            RefData.ID.Put( Int2Str((int)temp), id );

            // Notifies about the creation of the referendum ID.
            Process(id, "The referendum process has started.");
            return id;
        }
        
        // The function to vote on a referendum is declared above.

        // --> read
        private static object GetRef( string rID, string opt = "hasresult" )
        {
            if ( opt == "proposal" ) return RefData.Proposal.Get(rID);
            else if ( opt == "notes" ) return RefData.Notes.Get(rID);
            else if ( opt == "cost" ) return RefData.Cost.Get(rID);
            else if ( opt == "address" ) return RefData.Address.Get(rID);
            else if ( opt == "time" ) return RefData.Time.Get(rID);
            else if ( opt == "moneyraised" ) return RefData.MoneyRaised.Get(rID);
            else if ( opt == "numofvotes" ) return RefData.NumOfVotes.Get(rID);
            else if ( opt == "counttrue" ) return RefData.CountTrue.Get(rID);
            else if ( opt == "outcome" ) return RefData.Outcome.Get(rID);
            else if ( opt == "starttime" ) return RefData.StartTime.Get(rID);
            else if ( opt == "endtime" ) return RefData.EndTime.Get(rID);
            else return RefData.HasResult.Get(rID);
        }
        
        // --> update
        // It is only possible to internally change the 'moneyraised', the 'numofvotes',
        // the 'counttrue', the 'hasresult' and the 'outcome' (polymorphism).
        private static void UpRef( string rID, string opt, BigInteger val )
        {
            if ( (opt == "numofvotes") || (opt == "moneyraised") || (opt == "counttrue") || (opt == "hasresult") )
            {
                BigInteger orig = (BigInteger)GetRef(rID, opt);
                
                // Doesn't invoke Put if value is unchanged.
                if ( orig == val ) return;
                
                if ( val == 0 )
                {
                    // Deletes the respective storage if the new value is zero.
                    if ( opt == "numofvotes" ) RefData.NumOfVotes.Delete(rID);
                    else if ( opt == "moneyraised" ) RefData.MoneyRaised.Delete(rID);
                    else if ( opt == "counttrue" ) RefData.CountTrue.Delete(rID);
                    else RefData.HasResult.Delete(rID);
                }
                else
                {
                    // Update the respective storage with the new value.
                    if ( opt == "numofvotes" ) RefData.NumOfVotes.Put(rID, val);
                    else if ( opt == "moneyraised" ) RefData.MoneyRaised.Put(rID, val);
                    else if ( opt == "counttrue" ) RefData.CountTrue.Put(rID, val);
                    else RefData.HasResult.Put(rID, val);
                }
            }
        }

        private static void UpRef( string rID, bool val )
        {
            bool orig = Str2Bool( (string)GetRef(rID, "outcome") );

            // Doesn't invoke Put if value is unchanged.
            if ( orig == val ) return;

            // else   
            RefData.Outcome.Put(rID, Bool2Str(val));
        }

        // --> delete
        // A referendum process remains forever... and ever.
        
        
        //---------------------------------------------------------------------------------------------
        // METHODS TO FINANCE A NEW POWER PLANT
        // --> create
        private static void CrowdFunding( string ppID )
        {
            // Stores the practical values.
            ICOData.StartTime.Put(ppID, InvokedTime());
            ICOData.EndTime.Put(ppID, InvokedTime() + timeFrameCrowd);
            ICOData.Success.Put(ppID, Bool2Str(false));

            // Just states the other values since it is expensive to store null values.
            // ICOData.TotalAmount.Put(ppID, 0);
            // ICOData.Contributions.Put(ppID, 0);
            // ICOData.HasResult.Put(ppID, 0);
        }

        // The function to bid on a crowdfunding is declared above.
        // However, the option 'ICOData.Bid.Put(bidID, value)' is only available through
        // the updating method, and not as part of the creation method.
        
        // --> read
        private static BigInteger GetBid( string ppID, byte[] member )
        {
            string bidID = ID( "\x27", false, new string[] {ppID, member.AsString()} );
            return ICOData.Bid.Get(bidID).AsBigInteger();
        }

        private static object GetCrowd( string ppID, string opt = "hasresult" )
        {
            if ( opt == "starttime" ) return ICOData.StartTime.Get(ppID);
            else if ( opt == "endtime" ) return ICOData.EndTime.Get(ppID);
            else if ( opt == "totalamount" ) return ICOData.TotalAmount.Get(ppID);
            else if ( opt == "contributions" ) return ICOData.Contributions.Get(ppID);
            else if ( opt == "success" ) return ICOData.Success.Get(ppID);
            else return ICOData.HasResult.Get(ppID);
        }
        
        // --> update
        private static void UpBid( string ppID, byte[] member, BigInteger bid )
        {
            BigInteger orig = GetBid(ppID, member);
            
            // Doesn't invoke Put if bid is unchanged OR the new value is zero.
            if ( (orig == bid) || (bid == 0) ) return;
            
            // else
            string bidID = ID( "\x27", false, new string[] {ppID, member.AsString()} );
            ICOData.Bid.Put( bidID, orig + bid );
        }

        // Only the 'totalamount', 'contributions', 'hasresult' and 'success' (polymorphism) can be updated.
        private static void UpCrowd( string ppID, string opt, BigInteger val )
        {
            if ( (opt == "totalamount") || (opt == "contributions") || (opt == "hasresult") )
            {
                BigInteger orig = (BigInteger)GetCrowd(ppID, opt);
                
                // Doesn't invoke Put if value is unchanged.
                if ( orig == val ) return;
                
                // Deletes the respective storage if the new value is zero.
                if ( val == 0 )
                {
                    if ( opt == "totalamount" ) ICOData.TotalAmount.Delete(ppID);
                    else if ( opt == "contributions" ) ICOData.Contributions.Delete(ppID);
                    else ICOData.HasResult.Delete(ppID);
                }
                else
                {
                    // Updates the respective storage with the new value.
                    if ( opt == "totalamount" ) ICOData.TotalAmount.Put(ppID, val);
                    else if ( opt == "contributions" ) ICOData.Contributions.Put(ppID, val);
                    else ICOData.HasResult.Put(ppID, val);
                }
            }
        }

        private static void UpCrowd( string ppID, bool val )
        {
            string orig = (string)GetCrowd(ppID, "success");
            
            // Doesn't invoke Put if value is unchanged.
            if ( orig == Bool2Str(val) ) return;
            
            // else
            ICOData.Success.Put(ppID, Bool2Str(val));
        }
        
        // --> delete
        private static void Cancel( string ppID, byte[] member )
        {
            BigInteger grant = GetBid(ppID, member);
            
            // Decreases the total amount of funds.
            BigInteger funds = (BigInteger)GetCrowd(ppID, "totalamount");
            UpCrowd(ppID, "totalamount", funds - grant);

            // Decreases the total number of contributions.
            BigInteger contributions = (BigInteger)GetCrowd(ppID, "contributions");
            UpCrowd(ppID, "contributions", contributions - 1);
            
            // Deletes the member's offer.
            string bidID = ID( "\x27", false, new string[] {ppID, member.AsString()} );
            ICOData.Bid.Delete(bidID);

            // Notifies about the cancel of the bid.
            Retract( ppID, member, 0, (-1 * grant) );
        }
        
        // A crowdfunding process remains forever... even if it fails.
        // In this case, only the 'totalamount' and 'contributions' will
        // be "deleted" through the function above.
    }
}
