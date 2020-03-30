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
        public static event Action<string, byte[], BigInteger, BigInteger> Return;
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


        //---------------------------------------------------------------------------------------------
        // GLOBAL VARIABLES

        // Power limits of the distributed generation category defined by Brazilian law (from 0MW to 5MW).
        public static int[] PowGenLimits() => new int[] {0, 5000000};

        // The total number of referendum processes.
        public static BigInteger NumOfRef() => Storage.Get("NumOfRef").AsBigInteger();

        // The total number of power plant units.
        public static BigInteger NumOfPP() => Storage.Get("NumOfPP").AsBigInteger();

        // The total number of members.
        public static BigInteger NumOfMemb() => Storage.Get("NumOfMemb").AsBigInteger();

        // The total power supply at the group, i.e., sum of PP's capacity.
        public static BigInteger TotalSupply() => Storage.Get("TotalSupply").AsBigInteger();

        // The number of days to answer a referendum process.
        private const uint timeFrameRef = 259200;   // 30 days

        // The time a given function is invoked.
        private static uint InvokedTime() => Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;

        // Token settings.
        public static string Name() => "Sharing Electricity in Brazil";
        public static string Symbol() => "SEB";
        public static byte Decimals() => 3;                                                         // {0, 5000}
        public static byte[] Owner() => ExecutionEngine.ExecutingScriptHash;                        // aka GetReceiver() -- this smart contract == this smart contract ScriptHash
        public static string[] SupportedStandards() => new string[] { "NEP-5", "NEP-7", "NEP-10" };

        // Member's dataset.
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

        // Referendum's dataset.
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

        // Power Plant's dataset.
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

        // ICO's dataset (for crowdfunding).
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

        // New Power Plant crowdfunding settings.
        private const uint factor = 1000;               // 1kW == 1SEB
        private const byte minOffer = 100;              // Brazilian Reais (R$)
        private const uint timeFrameCrowd = 518400;     // 60 days
        private const uint minTimeToMarket = 259200;    // 30 days

        // The restrictive message to show up.
        private static Exception Warning() => new InvalidOperationException("Only members can access this information. Join us!");

        // To lock the registering process without voting.
        private static void OnlyOnce() => Storage.Put("firstCall", 1);

        // Caller identification.
        public static byte[] Caller() => "0".AsByteArray(); // --PENDING!-- isso está errado e 'ExecutionEngine.CallingScriptHash' não funciona direito

        // Trick to support the conversion from 'int' to 'string'.
        private static string[] Digits() => new string[10] {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9"};

        // Trick to get the type of a 'string' (and of a 'integer').
        private static char[] Alpha() => new char[] {'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'};

        // The characters of the Base58 scheme.
        private const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        //---------------------------------------------------------------------------------------------
        // THE MAIN INTERFACE

        public static object Main ( string operation, params object[] args )
        {
            // General operation.
            if (operation == "admission")
            {
                if ( args.Length != 3 )
                    throw new InvalidOperationException("Please provide the 3 arguments: your account address, full name, and power utility name.");

                if ( !Runtime.CheckWitness((byte[])args[0]) )
                    throw new InvalidOperationException("The admission can not be done on someone else's behalf.");

                if ( ( (string)GetMemb((byte[])args[0]) ).Length != 0 )
                    throw new InvalidOperationException("Thanks, you're already a member. We're glad to have you as part of the group!");
                
                if ( Storage.Get("firstCall").AsBigInteger() == 0 )
                {
                    // No admission process is required.

                    // Locks this 'if' statement.
                    OnlyOnce();
                    
                    // Defines the 'invoker/caller' as the first member.
                    Membership( (byte[])args[0], "Welcome on board!" );
                    Member( (byte[])args[0], (string)args[1], (string)args[2], 0, 0 );
                    return true;
                }

                return Admission( (byte[])args[0],   // invoker/caller address
                                  (string)args[1],   // fullName
                                  (string)args[2] ); // utility
            }
            
            // Partially restricted operation.
            if (operation == "summary")
            {
                if ( args.Length < 1 )
                    throw new InvalidOperationException("Provide at least a member address or a PP ID.");

                if ( (((string)GetMemb(Caller())).Length == 0) && (((string)GetMemb((byte[])args[0])).Length != 0) ) // definir o caller é foda! --PENDING-- posso usar o VerifySignature?
                    throw Warning();

                return Summary( (object)args[0],     // any ID
                                (string)args[1] );   // option
            }

            // Restricted operations.
            if ( GetMemb(Caller()).AsString().Length != 0 )
            {
                // Group operations.
                if (operation == "vote")
                {
                    if ( args.Length != 3 )
                        throw new InvalidOperationException("Please provide the 3 arguments: the referendum id, your account address, and your vote.");

                    if ( !Runtime.CheckWitness((byte[])args[1]) )
                        throw new InvalidOperationException("The vote can not be done on someone else's behalf.");

                    if ( isLock( (string)args[0]) )
                        throw new InvalidOperationException("The ballot has ended.");
                    
                    return Vote( (string)args[0],    // referendum id
                                 (byte[])args[1],    // member address
                                 (bool)args[2] );    // answer
                }

                if (operation == "bid")
                {
                    if ( args.Length != 3 )
                        throw new InvalidOperationException("Please provide the 3 arguments: the PP id, your account address, and your bid.");

                    if ( !Runtime.CheckWitness((byte[])args[1]) )
                        throw new InvalidOperationException("The bid can not be done on someone else's behalf.");

                    if ( (((string)args[0])[0] != 'P') || (((string)args[0]).Length == 0) )
                        throw new InvalidOperationException("Provide a valid PP ID.");

                    if ( (GetPP((string)args[0], "Utility")) != (GetMemb((byte[])args[1], "Utility")) )
                        throw new InvalidOperationException("This member cannot profit from this power utility." );

                    if ( (byte)args[2] <= minOffer )
                        throw new InvalidOperationException(String.Concat("The minimum bid allowed is R$ ", Int2Str(minOffer)));
                    
                    if ( isLock( (string)args[0] ) )
                        throw new InvalidOperationException("The campaign has ended.");

                    return Bid( (string)args[0],        // PP id
                                (byte[])args[1],        // member address
                                (BigInteger)args[2] );  // bid value
                }

                if (operation == "trade")
                {
                    if ( args.Length != 4 )
                        throw new InvalidOperationException("Please provide the 4 arguments: your account address, the address of who you are transaction to, the quota value, and the amount of tokens.");

                    if ( !Runtime.CheckWitness((byte[])args[0]) )
                        throw new InvalidOperationException("Only the owner of an account can exchange her/his asset.");
                    
                    if ( ((string)GetMemb((byte[])args[1])).Length == 0 )
                        throw new InvalidOperationException("The address you are transaction to must be a member too.");

                    if ( (GetMemb((byte[])args[0], "Utility")) != (GetMemb((byte[])args[1], "Utility")) )
                        throw new InvalidOperationException( "Both members must belong to the same power utility cover area." );

                    if ( ((int)args[2] <= 0) & ((int)args[3] <= 0) )
                        throw new InvalidOperationException("You're doing it wrong. To donate energy let ONLY the 4th argument empty. Otherwise, to donate tokens let ONLY the 3rd argument empty.");
                    
                    return Trade( (byte[])args[0],       // from address
                                  (byte[])args[1],       // to address
                                  (BigInteger)args[2],   // quota exchange
                                  (BigInteger)args[3] ); // token price
                }

                if (operation == "power up")
                {
                    if (args.Length != 4)
                        throw new InvalidOperationException("Please provide the 4 arguments: the PP capacity, the cost to build it up, the power utility name in which the PP will be installed, and the period to wait the new PP gets ready to operate.");

                    if ( ((int)args[3] == 0) || ((int)args[3] < minTimeToMarket) )
                        throw new InvalidOperationException("The time to market must be a factual period.");

                    return PowerUp( (int)args[0],       // capacity [MW]
                                    (int)args[1],       // cost [R$]
                                    (string)args[2],    // power utility name
                                    (uint)args[3] );    // time to market
                }

                if (operation == "change")
                {
                    if (args.Length != 2)
                        throw new InvalidOperationException("Please provide 2 arguments only. The first one must be the identification of the member (address) or the PP (id). The second one must be an array. It can be either the options about the data that will be changed, or an empty array to request the delete of something.");
                    
                    if ( (args[0] is string) & (((string)args[0])[0] == 'P') ) // Must be a PP ID.
                    {
                        if ( ((string)GetPP((string)args[0], "utility")).Length == 0 )
                            throw new InvalidOperationException("Provide a valid PP ID.");

                        var opt = (object[])args[1];

                        if ( opt.Length > 2 )
                            throw new InvalidOperationException("A maximum of 2 options are required to update a PP subject.");

                        // Updates PP utility name.
                        if ( (opt.Length == 1) & !(opt[0] is string) )
                            throw new InvalidOperationException("Provide a valid power utility name format to be replaced by.");
                        
                        // Updates the bid on a PP crowdfunding campaing.
                        if ( opt.Length == 2 )
                        {
                            if ( isLock( (string)args[0] ) )
                                throw new InvalidOperationException("The campaign has ended.");

                            if ( !(Runtime.CheckWitness((byte[])opt[0])) )
                                throw new InvalidOperationException("Only the member can change its bid.");
                        }
                    }
                    else // Should be a member ID (address).
                    {
                        if ( ((string)GetMemb((byte[])args[0])).Length == 0 )
                            throw new InvalidOperationException("Provide a valid member address.");

                        if ( (((object[])args[1]).Length != 2) || (((object[])args[1]).Length != 0) )
                            throw new InvalidOperationException("Provide valid arguments to update/delete an address.");

                        if ( ( ((string)((object[])args[1])[0] == profile[0]) | ((string)((object[])args[1])[0] == profile[1]) ) & !(Runtime.CheckWitness((byte[])args[0])) )
                            throw new InvalidOperationException("Only the member can change her/his profile data.");
                    }                    
                    
                    return Change( (object)args[0],     // any ID
                                   (object[])args[1] ); // array with desired values
                }
                
                // Administrative operations.
                if (operation == "admission result")
                {
                    if ( args.Length != 1 )
                        throw new InvalidOperationException("Please provide only the admission process ID.");
                    
                    if ( isLock( (string)args[0] ) )
                        throw new InvalidOperationException("There isn't a result yet.");
                    
                    return AdmissionResult( (string)args[0] ); // Referendum ID
                }
                
                if (operation == "change result")
                {
                    if ( args.Length != 1 )
                        throw new InvalidOperationException("Please provide only the change process ID.");
                    
                    if ( isLock( (string)args[0] ) )
                        throw new InvalidOperationException("There isn't a result yet.");
                    
                    ChangeResult( (string)args[0] ); // Referendum ID
                }
                
                if (operation == "power up result")
                {
                    if ( args.Length == 0 )
                        throw new InvalidOperationException("Please provide at least the new PP process ID.");
                        
                    if ( args.Length > 2 )
                        throw new InvalidOperationException("Please provide at most the new PP process ID, and the PP ID itself if any.");
                    
                    PowerUpResult( (string)args[0],     // Referendum ID
                                   (string)args[1] );   // PP ID
                }

                if (operation == "list of power plants")
                {
                    if ( args.Length != 0 )
                        throw new InvalidOperationException("This function does not need attributes.");
                    
                    foreach (byte[] ppID in ListOfPPs())
                    {
                        Runtime.Notify( ppID );
                    }
                }

                if (operation == "list of members")
                {
                    if ( args.Length != 0 )
                        throw new InvalidOperationException("This function does not need attributes.");
                    
                    foreach (byte[] address in ListOfMembers())
                    {
                        Runtime.Notify( address );
                    }
                }
            }

            throw new InvalidOperationException("No operation found. Have you wrote it right?");
        }


        //---------------------------------------------------------------------------------------------
        // GROUP FUNCTIONS - The restrictions are made on the 'Main'.

        // To request to join the group.
        public static string Admission( byte[] address, string fullName, string utility )
        {
            string id = Ref( fullName, utility, address );
            Membership( address, "Request for admission." );
            return id;
        }

        // To get information about something.
        public static object Summary( object id, string opt = "" )
        {
            // If 'id' is a 'byte[]' ==  member.
            if (!(id is string))
            {
                var address = (byte[])id;
                
                if ((opt == "") || (opt == "detailed"))
                {
                    object[] brief = new object[] { GetMemb(address), GetMemb(address,"utility"), GetMemb(address,"quota"), GetMemb(address,"tokens") };

                    if (opt == "detailed")
                    {
                        ShowContributedValues( address, ListOfPPs() );
                    }
                    return brief;
                }
                return GetMemb(address,opt);
            }

            // If 'id' is a 'string' with prefix 'P' == power plant.
            else if (((string)id)[0] == 'P')
            {
                var ppID = (string)id;
                
                // The PP's crowdfunding had succeed and the PP is operating.
                if ( (bool)GetPP(ppID) )
                {
                    if ( (opt == "") || (opt == "detailed") )
                    {
                        object[] brief = new object[] { GetPP(ppID,"Capacity"), GetPP(ppID,"Cost"), GetPP(ppID,"Utility"), GetPP(ppID,"TotMembers") };
            
                        if (opt == "detailed")
                        {
                            ShowContributedValues( ppID, ListOfMembers() );
                        }
                        return brief;
                    }
                    return GetPP(ppID,opt);
                }
                
                // The PP's crowdfunding may be succeed or not, and the PP is definitely not operating.
                else
                {
                    if ( (opt == "") || (opt == "detailed") )
                    {
                        object[] brief = new object[] { GetCrowd(ppID,"Start Time"), GetCrowd(ppID,"End Time"), GetCrowd(ppID,"Total Amount"), GetCrowd(ppID,"Contributions"), GetCrowd(ppID,"Success") };

                        if (opt == "detailed")
                        {
                            // for (int num = 0; num < NumOfMemb(); num++)
                            foreach (byte[] member in ListOfMembers())
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
            else if (((string)id)[0] == 'R')
            {
                var rID = (string)id;
                
                if (opt == "")
                {
                    return new object[] { GetRef(rID,"Proposal"), GetRef(rID,"Notes"), GetRef(rID,"Cost"), GetRef(rID,"Outcome") };
                }
                return GetRef(rID,opt);
            }

            // Wrap-up the group information.
            else
            {
                return new object[] { PowGenLimits()[0], PowGenLimits()[1], NumOfPP(), NumOfMemb(), Name(), Symbol(), TotalSupply() };
            }
        }

        // To vote in a given ID process.
        public static bool Vote( string id, byte[] member, bool answer )
        {
            // Increases the number of votes.
            BigInteger temp = (BigInteger)GetRef(id,"Num of Votes");
            UpRef(id, "Num of Votes", temp+1);

            if (answer)
            {
                // Increases the number of "trues".
                temp = (BigInteger)GetRef(id,"Count True");
                UpRef(id, "Count True", temp+1);
            }

            // Publishes the vote.
            Ballot(id, member, answer);

            return answer;
        }

        // To make a bid in a new PP crowdfunding process.
        public static bool Bid( string id, byte[] member, BigInteger bid )
        {
            BigInteger target = (BigInteger)GetPP(id, "cost");
            BigInteger funds = (BigInteger)GetCrowd(id, "totalamount");
            
            if ( bid > (target - funds) )
                throw new InvalidOperationException( String.Concat(String.Concat("You offered more than the amount available (R$ ", Int2Str( (int)(target - funds) ), ",00). Bid again!" ));

            // WARNING!
            // All these steps are part of a crowdfunding process, not of a PP registration.
            
            // Increases the value gathered so far.
            UpCrowd(id, "totalamount", funds + bid);
            
            // Increases the number of contributions.
            BigInteger temp = (BigInteger)GetCrowd(id, "contributions");
            UpCrowd(id, "contributions", temp+1);
            
            // Tracks bid by member for each ICO process.
            UpBid(id, member, bid);
            Offer(id, member, bid);
            
            return true;
            
            // If the hole fund process succeed, the money bid must be converted to percentage (bid/cost),
            // so it will be possible to define the quota and the SEB a member has to gain.
            // It is made on PowerUpResult(...).
        }

        // To update something on the ledger.
        public static object Change( object id, params object[] opts )
        {
            string rID;

            // If 'id' is a 'byte[]' ==  member.
            if (!(id is string))
            {
                // Only the member can change its own personal data.
                // To UPDATE, the params must be ['profile option', 'value'].
                if ( opts[1] is string )
                {
                    UpMemb((byte[])id, (string)opts[0], (string)opts[1]);
                    Update("Profile data.", id);
                    return true;
                }
                
                // Any member can request the change of registration data of other member.
                // To UPDATE, the params must be ['register option', 'value'].
                if ( opts[1] is BigInteger )
                {
                    rID = Ref( "Change register_", (string)opts[0], (byte[])id, (int)opts[1] );
                    Process( rID, "Request the change of registration data of a member." );
                    return rID;
                }
                
                // Any member can request to delete another member.
                // The 'opts.Length' is empty.
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
                Process( rID, "Request the change of utility name of a PP." );
                return rID;
            }

            // Any member can request to DELETE a PP.
            // The 'opts.Length' is empty.
            rID = Ref("Delete PP_", null, ((string)id).AsByteArray());
            Process(rID, "Request to delete a PP.");
            return rID;
        }

        // The whole process to integrate a new PP on the group power generation.
        public static string PowerUp( int capacity, int cost, string utility, uint timeToMarket )
        {
            string id = Ref( Int2Str(capacity), utility, new byte[1] {0x00}, cost, timeToMarket );
            Process( id, "Request to add a new PP." );
            return id;
        }

        // To allow the transfer of shares/tokens from someone to someone else (transactive energy indeed).
        // The 'fromAddress' will exchange an amount of shares with 'toAddress' by a defined token price,
        // i.e., while 'fromAddress' sends shares to 'toAddress', the 'toAddress' sends tokens to 'fromAddress'.
        public static bool Trade( byte[] fromAddress, byte[] toAddress, BigInteger exchange, BigInteger price )
        {
            int n = 2;
            BigInteger[] toWallet = new BigInteger[n];
            BigInteger[] fromWallet = new BigInteger[n];
            
            // register = {"quota", "tokens"}
            for (int r = 0; r < n; r++)
            {
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
        // After a period of 'timeFrameRef' days, a member should invoke those functions to state the referendum process.
        // An off-chain operation should handle this waiting time.

        public static bool AdmissionResult( string id )
        {
            // Updates the result.
            CalcResult(id);
            
            // Retrives the address from private storage.
            byte[] address = (byte[])GetRef(id, "address");

            if ( Str2Bool( (string)GetRef(id, "outcome") ) )
            {
                // Retrives the member data from private storage.
                string fullName = (string)GetRef(id, "proposal");
                string utility = (string)GetRef(id, "notes");

                // Adds a new member after the group approval.
                Member( address, fullName, utility, 0, 0 );
                Membership( address, "Welcome on board!" );
                return true;
            }
            // Otherwise, leave the user out of the group.
            Membership( address, "Not approved yet." );
            return false;
        }

        public static bool ChangeResult( string id )
        {
            // Updates the result.
            CalcResult(id);
            
            if ( Str2Bool( (string)GetRef(id, "outcome") ) )
            {
                Process(id, "Approved.");

                // Identifies the proposal and does the respective operation.
                string proposal = (string)GetRef(id, "proposal");

                if (proposal == "Change register_")
                {
                    byte[] key = (byte[])GetRef(id, "address");
                    UpMemb(key, (string)GetRef(id, "notes"), (BigInteger)GetRef(id, "cost"));
                    Update("Registration data.", key);
                }
                            
                if (proposal == "Delete member_")
                {
                    byte[] key = (byte[])GetRef(id, "address");
                    BigInteger portion = (BigInteger)GetMemb(key, "Quota");
                    BigInteger give_out = portion/(NumOfMemb() - 1);
                    
                    foreach (byte[] member in ListOfMembers())
                    {
                        // In an infinitesimal period of time the group will be disbalanced
                        // until the related member be completely deleted.
                        // There is no side effect and it is better than iterate through each member.
                        
                        Distribute(member, give_out, 0);
                    }
            
                    DelMemb(key);
                    Membership(key, "Goodbye.");
                }
                
                if (proposal == "Change utility_")
                {
                    UpPP(id, "utility", (string)GetRef(id, "notes"));
                    Update("Belonging of.", id);
                }
                
                if (proposal == "Delete PP_")
                {
                    DelPP(id);
                    Update("Deletion of.", id);
                }

                return true;
            }

            Process(id, "Denied.");
            return false;
        }

        public static object PowerUpResult( string id, string ppID = null )
        {
            // STEP 1 - Analyzes the referendum about the request for a new PP.
            if (ppID == null)
            {
                if ( isLock(id) )
                    throw new InvalidOperationException("There isn't a result about the new PP request yet.");
                
                // After a 'timeFrameRef' waiting period...

                // Evaluates the referendum result only once.
                if ( (BigInteger)GetRef(id) == 0 )
                {
                    // Updates the result.
                    CalcResult(id);
                    
                    if ( Str2Bool( (string)GetRef(id, "outcome") ) )
                    {
                        // Referendum has succeeded. It's time to add a new PP.
                        
                        // Gets the terms from the begining of the process.
                        string capacity = (string)GetRef(id, "proposal");
                        BigInteger cost = (BigInteger)GetRef(id, "cost");
                        string utility = (string)GetRef(id, "notes");
                        uint timeToMarket = (uint)GetRef(id, "time");
                        
                        // Generates the PP ID.
                        string PPid = PP(capacity, cost, utility, timeToMarket);
                        
                        // Starts to raise money for it.
                        CrowdFunding(PPid);
                        Process(PPid, "Shut up and give me money!");
                        return PPid;
                    }
                    
                    // Otherwise...
                    Process(id, "This PP was not approved yet. Let's wait a bit more.");
                    return false;
                }
                
                return "This process step is completed.";
            }
            
            // STEP 2 - Analyzes the crowdfunding of the new PP approved.
            if ( isLock(ppID) )
                throw new InvalidOperationException("There isn't a result about the new PP crowdfunding yet.");
            
            // After a 'timeFrameCrowd' waiting period...

            // Evaluates the crowdfunding result only once.
            if ( (BigInteger)GetCrowd(ppID) == 0 )
            {                
                // Updates the result.
                UpCrowd(ppID, "hasresult", 1);

                // Gets the values from the crowdfunding process.
                BigInteger target = (BigInteger)GetPP(ppID, "cost");
                BigInteger funding = (BigInteger)GetCrowd(ppID, "totalamount");

                // Evaluates if the building of the new PP starts or not.
                if (funding == target)
                {
                    // Crowdfunding has succeeded.
                    UpCrowd(ppID, true);
                    
                    // Updates the number of investors.
                    UpPP(ppID, "numOfFundMemb", ListOfFunders(ppID).Length);
                    
                    Process(id, "New power plant on the way.");
                    return true;
                }
                
                // Otherwise, the "success" remains as 'false'.
                foreach (byte[] funder in ListOfFunders(ppID))
                {
                    Refund(ppID, funder);
                }
                
                Process(id, "Fundraising has failed.");
                return false;
            }
            
            // STEP 3 - Analyzes the PP operation status.

            // Calculates the date the new PP is planned to start to operate, that can always be updated until the deadline.
            // operationDate = ICO_endTime + PP_timeToMarket
            uint operationDate = (uint)GetCrowd(ppID, "endtime") + (uint)GetPP(ppID, "timetomarket");
            
            if ( InvokedTime() <= operationDate )
                throw new InvalidOperationException("The new PP is not ready to operate yet.");
            
            // After waiting for the time to market.

            // Evaluates the construction only once.
            if ( (BigInteger)GetPP(ppID) == 0 )
            {
                // When the PP is ready to operate, it's time to distribute tokens and shares.

                // Increases the total power supply of the group.
                BigInteger capOfPP = (BigInteger)GetPP(ppID, "capacity");       // [MW]
                BigInteger capOfGroup = TotalSupply() + capOfPP;                // [MW]
                Storage.Put("TotalSupply", capOfGroup);
            
                // Identifies how much the new Power Plant takes part on the group total power supply.
                BigInteger sharesOfPP = capOfPP/capOfGroup;                     // [pu]
                
                foreach (byte[] funder in ListOfFunders(ppID))
                {
                    // Gets the member contribution.
                    BigInteger grant = GetBid(ppID, funder);                    // [R$]

                    // Identifies the member participaction rate.
                    BigInteger rate = grant/((BigInteger)GetRef(ppID, "cost")); // [pu]
                    
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
            
            for (int num = 0; num < NumOfPP(); num++)
            {
                var ppID = PPData.ID.Get( Int2Str(num+1) );
                ppIDs[num] = ppID;
            }
            return ppIDs;
        }

        // To return the address of each member.
        private static byte[][] ListOfMembers()
        {
            byte[][] addresses = new byte[ (int)NumOfMemb() ][];
            
            for (int num = 0; num < NumOfMemb(); num++)
            {
                var memberAddress = MemberData.ID.Get( Int2Str(num+1) );
                addresses[num] = memberAddress;
            }
            return addresses;
        }

        // To return a list of members that have financed a new PP.
        private static byte[][] ListOfFunders( string ppID )
        {
            byte[][] funders = new byte[ (int)GetCrowd(ppID, "contributions") ][];

            int num = 0;
            foreach (byte[] member in ListOfMembers())
            {
                var bid = GetBid( ppID, member );
                
                if (bid != 0)
                {
                    funders[num] = member;
                    num++;
                }
            }
            return funders;
        }


        //---------------------------------------------------------------------------------------------
        // SYSTEM FUNCTIONS

        // A new PP will only distribute tokens and shares after a crowdfunding process succeed.
        // All the exceptions were handle during the crowdfunding. It only needs to distribute the assets.
        private static void Distribute( byte[] toAddress, BigInteger quota, BigInteger tokens )
        {
            BigInteger[] pastWallet = new BigInteger[ register.Length ];
            int num = 0;
            
            // register = {"Quota", "Tokens"}
            foreach (string data in register)
            {
                pastWallet[num] = ( (BigInteger)GetMemb(toAddress, data) );
                num++;
            }
            
            UpMemb(toAddress, register[0], pastWallet[0] + quota);
            UpMemb(toAddress, register[1], pastWallet[1] + tokens);
            Transfer(null, toAddress, quota, tokens);
        }

        // To create a custom ID of a process based on its particular specifications.
        private static string ID( string prefix, params string[] args )
        {
            // Assuming that all operations are little-endian.
            
            // STEP 1 - HASH
            string data = Int2Str((int)InvokedTime());
            foreach( string a in args )
            {
                data = String.Concat(data,a);
            }
            byte[] scriptHash = Hash160( data.AsByteArray() );          // length = 20 bytes
            
            // STEP 2 - Enlarge the array to get the desired BigInteger's numbers range.
            byte[] temp = scriptHash.Take(1);
            scriptHash = scriptHash.Concat(temp);                       // length = 21 bytes
            
            // STEP 2 - PREFIX
            byte[] preID = scriptHash.Concat( prefix.AsByteArray() );   // length = 22 bytes
            
            // STEP 3 - BASE58
            return Encode58( preID );
        }

        // To properly store a boolean variable.
        private static string Bool2Str( bool val )
        {
            if (val) return "1";
            return "0";
        }

        // To properly read a boolean from storage.
        private static bool Str2Bool( string val )
        {
            if (val == "1") return true;
            return false;
        }

        // To affordably convert a integer to a string.
        private static string Int2Str(int num, string s = null)
        {
            if (num == 0) return s;

            int quotient = num / 10;
            int remainder = num % 10;
            
            string trick = Digits()[ remainder ];
                
            return Int2Str(quotient, String.Concat(trick, s) );
        }

        // The Base58 enconding scheme.
        private static string Encode58(byte[] preID)
        {
            // Restricts to positive values.
            byte[] data = preID.Concat("\x00".AsByteArray());   // length = 23 bytes
            
            // Converts byte[] to BigInteger and then to int.
            int input = (int)data.ToBigInteger();
            
            // Converts BigInteger to Base58.
            int[] result = new int[40]; // Big value to avoid constraints.
            
            int basis = 58;
            int pos = 0;
            int quotient = basis + 1;
            while (quotient > basis)
            {
                quotient = input / basis;
                result[pos] = input % basis;
                input = quotient;
                pos++;
            }
            result[pos] = input;
            
            string b58 = null;
            for (int k=pos; k >= 0; k--)
            {
                b58 += Alphabet[ result[k] ];
            }
            
            return b58;
        }

        // To filter the relationship of members and PPs.
        // Displays how much a member has contributed to a PP crowdfunding.
        private static void ShowContributedValues( object lookForID, object[] listOfIDs )
        {
            BigInteger bid;

            if (lookForID is string) // --PENDING!-- não sei se isso vai funcionar direito!
            {
                // 'lookForID' is a PP ID.

                // Gets members' bid by a PP funding process.
                foreach (byte[] member in listOfIDs)
                {
                    bid = GetBid((string)lookForID, member);
                    
                    if (bid != 0)
                    {
                        Runtime.Notify( new object[] { member, bid } );
                    }
                }
            }
            else
            {
                // 'lookForID' is a member ID.

                // Gets PPs by a member investments.
                foreach (string ppID in listOfIDs)
                {
                    bid = GetBid(ppID, (byte[])lookForID);
                    
                    if (bid != 0)
                    {
                        Runtime.Notify( new object[] { ppID, bid } );
                    }
                }
            }
        }

        // To calculate the referendum result only once.
        private static void CalcResult( string id )
        {
            if ( Str2Bool( (string)GetRef(id) ) )
            {
                BigInteger totalOfVotes = (BigInteger)GetRef(id, "numofvotes");
                BigInteger totalOfTrues = (BigInteger)GetRef(id, "counttrue");
                    
                if ( totalOfTrues > (totalOfVotes / 2) )
                {
                    // Referendum has succeeded.
                    UpRef(id, true);
                }                
                // Otherwise, the "outcome" remains as 'false'.

                // Certifies the calculation happens only once.
                UpRef(id, "hasresult", 1);
            }
        }

        // Actualy, it restricts a given operation to happen based on a timestamp.
        // Before a given time frame, no one is allowed to continue the process.
        // The monitoring of the time happens off-chain.
        // Once the time stated is reached, any member can then resume the process.
        private static bool isLock( string id )
        {
            uint endTime;
            
            if (id[0] == 'R')
            {
                endTime = (uint)GetRef(id, "endtime");
            }
            
            // else id[0] == 'P'
            endTime = (uint)GetCrowd(id, "endtime");
            
            if (InvokedTime() <= endTime) return true;
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
            BigInteger temp = NumOfMemb() + 1;
            Storage.Put("NumOfMemb", temp);
            
            // Stores the address of each member.
            MemberData.ID.Put( Int2Str((int)temp), address );
        }

        // --> read
        private static object GetMemb( byte[] address, string opt = "fullname" )
        {
            if (opt == "utility") return MemberData.Utility.Get(address);
            else if (opt == "quota") return MemberData.Quota.Get(address);
            else if (opt == "tokens") return MemberData.Tokens.Get(address);
            else return MemberData.FullName.Get(address);
        }

        // --> update
        // Detailed restrictions to update 'profile' or 'register' data are set
        // on the function 'Change'. Here this feature is handled by polymorphism.
        private static bool UpMemb( byte[] address, string opt, string val )
        {
            // Don't invoke Put if value is unchanged.
            string orig = (string)GetMemb(address, opt);
            if (orig == val) return true;
             
            // Use Delete rather than Put if the new value is empty.
            if (val.Length == 0) DelMemb(address, opt);
            return true;
            
            // else
            if (opt == "fullname") MemberData.FullName.Put(address, val);
            if (opt == "utility") MemberData.Utility.Put(address, val);

            return true;
        }

        private static bool UpMemb( byte[] address, string opt, BigInteger val )
        {
            // Don't invoke Put if value is unchanged.
            BigInteger orig = (BigInteger)GetMemb(address, opt);
            if (orig == val) return true;
             
            // Use Delete rather than Put if the new value is zero.
            if (val == 0) DelMemb(address, opt);
            return true;
            
            // else
            if (opt == "quota") MemberData.Quota.Put(address, val);
            if (opt == "tokens") MemberData.Tokens.Put(address, val);

            return true;
        }

        // --> delete
        private static void DelMemb( byte[] address, string opt = "" )
        {
            // To support an economic action for the update method.
            if (opt == "fullname") MemberData.FullName.Delete(address);
            else if (opt == "utility") MemberData.Utility.Delete(address);
            else if (opt == "quota") MemberData.Quota.Delete(address);
            else if (opt == "tokens") MemberData.Tokens.Delete(address);
            else
            {
                // The member exits the group (opt == "").

                foreach ( string option in new string[]{"fullname", "utility", "quota", "tokens"} )
                {
                    DelMemb(address, option);
                }
                
                // Looks for the member 'key' (that may vary during the life cycle of the group).
                for (int num = 1; num < NumOfMemb()+1; num++)
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
                Storage.Put("NumOfMemb", temp);
            }
        }


        //---------------------------------------------------------------------------------------------
        // METHODS FOR POWER PLANTS
        // --> create
        private static string PP( string capacity, BigInteger cost, string utility, uint timeToMarket )
        {
            // Creates the unique identifier.
            string id = ID( "\x53", new string[] {capacity, Int2Str((int)cost), utility, Int2Str((int)timeToMarket)} );

            // Stores the practical values.
            PPData.Capacity.Put(id, capacity);
            PPData.Cost.Put(id, cost);
            PPData.Utility.Put(id, utility);
            PPData.TimeToMarket.Put(id, timeToMarket);

            // Just states the other values since it is expensive to store null values.
            // PPData.NumOfFundMemb.Put(id, 0);
            // PPData.HasStarted.Put(id, 0);

            // Increases the total number of power plant units.
            BigInteger temp = NumOfPP() + 1;
            Storage.Put("NumOfPP", temp);
            
            // Stores the ID of each PP.
            PPData.ID.Put( Int2Str((int)temp), id );

            Process(id, "New PP created.");
            return id;
        }

        // --> read
        private static object GetPP( string id, string opt = "hasstarted" )
        {
            if (opt == "capacity") return PPData.Capacity.Get(id);
            else if (opt == "cost") return PPData.Cost.Get(id);
            else if (opt == "utility") return PPData.Utility.Get(id);
            else if (opt == "timetomarket") return PPData.TimeToMarket.Get(id);
            else if (opt == "numoffundmemb") return PPData.NumOfFundMemb.Get(id);
            else return PPData.HasStarted.Get(id);
        }

        // --> update
        // The 'utility', the 'hasstarted', and the 'timetomarket' are the only options that can be changed.
        // However, the 'utility' can be changed anytime, the 'hasStarted' can be changed only once, while
        // the 'timetomarket' is restricted by its deadline of start operation date.
        // To update the other options, delete the current PP and create a new one.
        private static void UpPP( string id, string opt, object val )
        {
            if (opt == "utility")
            {
                // Doesn't invoke Put if value is unchanged.
                string orig = (string)GetPP(id, "utility");
                if (orig == (string)val) return;
                
                // Does nothing if the new value is empty.
                if (((string)val).Length == 0) return;
                
                // else
                PPData.Utility.Put(id, (string)val);
                // WARNING: Logic constraints!
                // Must updates each member utility name as well.
                // However, only the member her/himself can change this information.
                // Therefore, 'utility' of both member's and PP's dataset must pointer to a common database.
                // THIS WAS NOT IMPLEMENTED!
            }
            
            if (opt == "hasstarted")
            {
                // Doesn't invoke Put if value is unchanged.
                BigInteger orig = (BigInteger)GetPP(id, "hasstarted");
                if (orig == (BigInteger)val) return;
                
                // Does nothing if the new value is empty.
                if ((BigInteger)val == 0) return;
                
                // else
                PPData.HasStarted.Put(id, (BigInteger)val);
            }
            
            if (opt == "timetomarket")
            {
                // Doesn't invoke Put if value is unchanged.
                uint orig = (uint)GetPP(id, "timetomarket");
                if (orig == (uint)val) return;
                
                // Does nothing if the new value is empty.
                if ((uint)val == 0) return;

                // Does nothing if the deadline has passed by.
                uint deadline = (uint)GetCrowd(id, "endtime") + (uint)GetPP(id, "timetomarket");
                
                if ( InvokedTime() > deadline )
                    throw new InvalidOperationException("The time has passed by. You can no longer postpone it.");
                
                // else
                PPData.TimeToMarket.Put(id, (uint)val);
            }
        }

        // --> delete
        private static void DelPP( string id )
        {
            PPData.Capacity.Delete(id);
            PPData.Cost.Delete(id);
            PPData.Utility.Delete(id);
            PPData.TimeToMarket.Delete(id);
            if ( (BigInteger)GetPP(id, "hasstarted") != 0 ) PPData.HasStarted.Delete(id);
            if ( (BigInteger)GetPP(id, "numoffundmemb") != 0 ) PPData.NumOfFundMemb.Delete(id);
            
            // Looks for the PP 'key' (that may vary during the life cycle of the group).
            for (int num = 1; num < NumOfPP()+1; num++)
            {
                var index = Int2Str(num);

                if ( id == PPData.ID.Get(index).AsString() )
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
            BigInteger temp = TotalSupply() - (BigInteger)GetPP(id, "Capacity");
            Storage.Put("TotalSupply", temp);

            // Decreases the total number of power plant units.
            temp = NumOfPP() - 1;
            Storage.Put("NumOfPP", temp);
        }


        //---------------------------------------------------------------------------------------------
        // METHODS FOR REFERENDUMS
        // --> create
        private static string Ref( string proposal, string notes, byte[] address, int cost = 0, uint time = 0 )
        {
            string id = ID( "\x5A", new string[] {proposal, notes, Int2Str(cost)} );

            // Stores the practical values.
            RefData.Proposal.Put(id, proposal);
            RefData.Notes.Put(id, notes);
            RefData.Outcome.Put(id, Bool2Str(false));
            RefData.StartTime.Put(id, InvokedTime());
            RefData.EndTime.Put(id, InvokedTime() + timeFrameRef);

            // Evaluates the values before stores them since it is expensive to store null values.
            if (address.Length != 0) RefData.Address.Put(id, address);
            if (cost != 0) RefData.Cost.Put(id, cost);
            if (time != 0) RefData.Time.Put(id, time);
            
            // Just states the other values since it is expensive to store null values.
            // RefData.MoneyRaised.Put(id, 0);
            // RefData.NumOfVotes.Put(id, 0);
            // RefData.CountTrue.Put(id, 0);
            // RefData.HasResult.Put(id, 0);
            
            // Increases the total number of referendum processes.
            BigInteger temp = NumOfRef() + 1;
            Storage.Put("NumOfRef", temp);
            
            // Stores the ID of each Ref.
            RefData.ID.Put( Int2Str((int)temp), id );

            Process(id, "The referendum process has started.");
            return id;
        }

        // The function to vote on a referendum is declared above because it is public.

        // --> read
        private static object GetRef( string id, string opt = "hasresult" )
        {
            if (opt == "proposal") return RefData.Proposal.Get(id);
            else if (opt == "notes") return RefData.Notes.Get(id);
            else if (opt == "cost") return RefData.Cost.Get(id);
            else if (opt == "address") return RefData.Address.Get(id);
            else if (opt == "time") return RefData.Time.Get(id);
            else if (opt == "moneyraised") return RefData.MoneyRaised.Get(id);
            else if (opt == "numofvotes") return RefData.NumOfVotes.Get(id);
            else if (opt == "counttrue") return RefData.CountTrue.Get(id);
            else if (opt == "outcome") return RefData.Outcome.Get(id);
            else if (opt == "starttime") return RefData.StartTime.Get(id);
            else if (opt == "endtime") return RefData.EndTime.Get(id);
            else return RefData.HasResult.Get(id);
        }

        // --> update
        // It is only possible to internally change the 'MoneyRaised', the 'NumOfVotes', the 'CountTrue', the 'HasResult' and the 'Outcome'.
        private static void UpRef( string id, string opt, BigInteger val )
        {
            if ((opt == "numofvotes") || (opt == "moneyraised") || (opt == "counttrue") || (opt == "hasresult") )
            {
                BigInteger orig = (BigInteger)GetRef(id, opt);
                
                if (orig == val)
                {
                    // Don't invoke Put if value is unchanged.
                }
                else if (val == 0)
                {
                    // Deletes the storage if the new value is zero.
                    if (opt == "numofvotes") RefData.NumOfVotes.Delete(id);
                    else if (opt == "moneyraised") RefData.MoneyRaised.Delete(id);
                    else if (opt == "counttrue") RefData.CountTrue.Delete(id);
                    else RefData.HasResult.Delete(id); // (opt == "hasresult")
                }
                else
                {
                    // Update the storage with the new value.
                    if (opt == "numofvotes") RefData.NumOfVotes.Put(id, val);
                    else if (opt == "moneyraised") RefData.MoneyRaised.Put(id, val);
                    else if (opt == "counttrue") RefData.CountTrue.Put(id, val);
                    else RefData.HasResult.Put(id, val); // (opt == "hasresult")
                }
            }
        }

        private static void UpRef( string id, bool val )
        {
            bool orig = Str2Bool( (string)GetRef(id, "outcome") );

            if ( orig == val )
            {
                // Don't invoke Put if value is unchanged.
            }
            else   
            {
                RefData.Outcome.Put(id, Bool2Str(val));
            }
        }

        // --> delete
        // A referendum process remains forever... and ever.


        //---------------------------------------------------------------------------------------------
        // METHODS TO FINANCE A NEW POWER PLANT
        // --> create
        private static void CrowdFunding( string id ) // This ID must come from a success Referendum process or it is a PP ID? --PENDING-- DEFINITION!
        {
            
            ICOData.StartTime.Put(id, InvokedTime());
            ICOData.EndTime.Put(id, InvokedTime() + timeFrameCrowd);
            // ICOData.TotalAmount.Put(id, 0); // Expensive to create with null value. Just state it out!
            // ICOData.Contributions.Put(id, 0); // Expensive to create with null value. Just state it out!
            ICOData.Success.Put(id, Bool2Str(false));
            // ICOData.HasResult.Put(id, 0); // Expensive to create with null value. Just state it out!
        }

        // The function to bid on a crowdfunding is declared above because it is public.

        // --> read
        private static BigInteger GetBid( string id, byte[] member )
        {
            byte[] bidID = Hash256( id.AsByteArray().Concat(member) );
            return ICOData.Bid.Get(bidID).AsBigInteger();
        }

        private static object GetCrowd( string id, string opt = "hasresult" )
        {
            if (opt == "starttime") return ICOData.StartTime.Get(id);
            else if (opt == "endtime") return ICOData.EndTime.Get(id);
            else if (opt == "totalamount") return ICOData.TotalAmount.Get(id);
            else if (opt == "contributions") return ICOData.Contributions.Get(id);
            else if (opt == "success") return ICOData.Success.Get(id);
            else return ICOData.HasResult.Get(id);
        }

        // --> update
        private static void UpBid( string id, byte[] member, BigInteger bid )
        {
            BigInteger orig = GetBid(id, member);
            
            if ((orig == bid) || (bid == 0))
            {
                // Don't invoke Put if value is unchanged.
                // AND
                // Keeps the storage with the original value.
            }
            else
            {
                byte[] bidID = Hash256( id.AsByteArray().Concat(member) );
                ICOData.Bid.Put( bidID, orig + bid );
            }
        }

        // Only the 'Total Amount', 'Contributions', 'HasResult' and 'Success' can be updated.
        private static void UpCrowd( string id, string opt, BigInteger val )
        {
            if ( (opt == "totalamount") || (opt == "contributions") || (opt == "hasresult") )
            {
                BigInteger orig = (BigInteger)GetCrowd(id, opt);
                
                if (orig == val)
                {
                    // Don't invoke Put if value is unchanged.
                }
                else if (val == 0)
                {
                    // Deletes the storage if the new value is zero.
                    if (opt == "totalamount") ICOData.TotalAmount.Delete(id);
                    else if (opt == "contributions") ICOData.Contributions.Delete(id);
                    else ICOData.HasResult.Delete(id); // (opt == "hasresult")
                }
                else
                {
                    // Update the storage with the new value.
                    if (opt == "totalamount") ICOData.TotalAmount.Put(id, val);
                    else if (opt == "contributions") ICOData.Contributions.Put(id, val);
                    else ICOData.HasResult.Put(id, val); // (opt == "hasresult")
                }
            }
        }

        private static void UpCrowd( string id, bool val )
        {
            string orig = (string)GetCrowd(id, "success");
            
            if ( orig == Bool2Str(val) )
            {
                // Don't invoke Put if value is unchanged.
            }
            else
            {
                ICOData.Success.Put(id, Bool2Str(val));
            }
        }

        // --> delete
        private static void Refund( string id, byte[] member )
        {
            BigInteger grant = GetBid(id, member);
            
            // Decreases the total amount of funds.
            BigInteger funds = (BigInteger)GetCrowd(id, "totalamount");
            UpCrowd(id, "totalamount", funds - grant);

            // Decreases the total number of contributions.
            BigInteger contributions = (BigInteger)GetCrowd(id, "contributions");
            UpCrowd(id, "contributions", contributions-1);
            
            // Deletes the member's offer.
            byte[] bidID = Hash256( id.AsByteArray().Concat(member) );
            ICOData.Bid.Delete(bidID);

            // Notifies about the cancel of the bid.
            Return(id, member, 0, (-1 * grant));
        }

        // Only the 'Total Amount' and 'Contributions' can be "deleted"
        // because the failure of a crowdfunding must be preserved.
        // Actually it is only used to "store" null values cheaply, and
        // it must solely happen if the refund (due to a bid cancel) reaches zero.
        private static void DelCrowd( string id, string opt )
        {
            if ( (opt == "totalamount") || (opt == "contributions") )
            {
                if (opt == "totalamount") ICOData.TotalAmount.Delete(id);
                else ICOData.Contributions.Delete(id); // (opt == "contributions")
            }
        }

    }
}
