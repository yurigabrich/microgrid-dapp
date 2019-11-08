//---------------------------------------------------------------------------------------------
// EVENTS

[DisplayName("transaction")]
public static event Action<string, string, BigInteger, BigInteger> Transfer;
[DisplayName("membership")]
public static event Action<string, string> Membership;
[DisplayName("process")]
public static event Action<string, string> Process;
[DisplayName("ballot")]
public static event Action<string, string, bool> Ballot;
[DisplayName("offer")]
public static event Action<string, string, BigInteger> Offer;
[DisplayName("change")]
public static event Action<string, string> Update;
[DisplayName("refund")]
public static event Action<string, BigInteger> Refund;


//---------------------------------------------------------------------------------------------
// GLOBAL VARIABLES

// Power limits of the distributed generation category defined by Brazilian law (from 0MW to 5MW).
public static int[] PowGenLimits() => new int[] {0, 5000000};

// The total number of power plant units.
public static int NumOfPP() => Storage.Get("NumOfPP");

// The total number of members.
public static int NumOfMemb() => Storage.Get("NumOfMemb");

// The total power supply at the group, i.e., sum of PP's capacity.
public static int TotalSupply() => Storage.Get("TotalSupply");

// The number of days to answer a referendum process.
private const uint timeFrameRef = 259200;   // 30 days

// The time a given function is invoked.
private static uint InvokeTime() => Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;

// Token settings.
public static string Name() => "Sharing Electricity in Brazil";
public static string Symbol() => "SEB";
public static byte Decimals() => 3;                                                         // {0, 5000}
public static byte[] Owner() => ExecutionEngine.ExecutingScriptHash;                        // aka GetReceiver() -- this smart contract
public static string[] SupportedStandards() => new string[] { "NEP-5", "NEP-7", "NEP-10" };

// Member's dataset.
private static string[] profile => new string[] {"FullName", "Utility"};
private static string[] register => new string[] {"Quota", "Tokens"};

// New Power Plant crowdfunding settings.
private const uint factor = 1000;               // Review at PowerUP() last operations --PENDING-- 1kW =?= 1SEB
private const byte minOffer = 100;              // Brazilian Reais (R$)
private const uint timeFrameCrowd = 518400;     // 60 days
private const uint minTimeToMarket = 259200;    // 30 days

// The restrictive message to show up.
private static Exception Warning() => new InvalidOperationException("Only members can access this information. Join us!");

// Caller authenticity...
public static byte[] Caller() => ...;                                                       // --PENDING--

// Trick to support the conversion from 'int' to 'string'.
private static string[] Digits() => new string[10] {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9"};

// Trick to get the type of a 'string' (and of a 'integer').
private static char[] Alpha() => new char[] {'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'};

//---------------------------------------------------------------------------------------------
// THE MAIN INTERFACE

public static object Main ( string operation, params object[] args )
{
    if ( Runtime.Trigger == TriggerType.Verification )
    {
        // Como garantir que o primeiro 'invoker' do contrato seja o primeiro membro e que isso aconteça somente 1 única vez? --PENDING--
        if ( Member.Get() == null )
        {
            if (args.Length != 2) return false;
            Member( caller, args[0], args[1], 100, 0 );
            return "New GGM blockchain initiated.";
        }

        return false;
    }
    else if ( Runtime.Trigger == TriggerType.Application )
    {
        // General operation.
        if (operation == "admission")
        {
            if ( args.Length != 3 )
                throw new InvalidOperationException("Please provide the 3 arguments: your account address, full name, and power utility name.");

            if ( !Runtime.CheckWitness((string)args[0]) ) // --PENDING-- aqui o args[0] deve ser byte[]...
                throw new InvalidOperationException("The admission can not be done on someone else's behalf.");

            if ( GetMemb((string)args[0], "FullName").Length != 0 )
                throw new InvalidOperationException("Thanks, you're already a member. We're glad to have you as part of the group!");
            
            return Admission( (string)args[0],   // invoker/caller address
                              (string)args[1],   // fullName
                              (string)args[2] ); // utility
        }
        
        // Partially restricted operation.
        if (operation == "summary")
        {
            if ( args.Length != 1 )
                throw new InvalidOperationException("Provide at least a member address or a PP ID.");

            if ( (GetMemb(caller, "FullName").Length == null) | (args[0][0] == "A") ) // definir o caller é foda! --PENDING-- posso usar o VerifySignature?
                throw Warning();

            return Summary( (string)args[0],     // Address/ID
                            (string)args[1] );   // option
        }

        // Restricted operations.
        if ( GetMemb(caller, "FullName").Length != null )
        {
            // Group operations.
            if (operation == "vote")
            {
                if ( args.Length != 3 )
                    throw new InvalidOperationException("Please provide the 3 arguments: the referendum id, your account address, and your vote.");

                if ( !Runtime.CheckWitness((string)args[0]) ) // --PENDING-- aqui o args[0] deve ser byte[]...
                    throw new InvalidOperationException("The vote can not be done on someone else's behalf.");

                if ( isLock( (string)args[0]) )
                    throw new InvalidOperationException("The ballot has ended.");
                
                return Vote( (string)args[0],    // referendum id
                             (string)args[1],    // member address
                             (bool)args[2] );    // answer
            }

            if (operation == "bid")
            {
                if ( args.Length != 3 )
                    throw new InvalidOperationException("Please provide the 3 arguments: the PP id, your account address, and your bid.");

                if ( !Runtime.CheckWitness((string)args[0]) ) // --PENDING-- aqui o args[0] deve ser byte[]...
                    throw new InvalidOperationException("The bid can not be done on someone else's behalf.");

                if ( (args[0][0] != "P") || (args[0].Length == null) )
                    throw new InvalidOperationException("Provide a valid PP ID.");

                if ( (GetPP(args[0], "Utility")) != (GetMemb(args[1], "Utility")) )
                    throw new InvalidOperationException("This member cannot profit from this power utility." );

                if ( args[2] <= minOffer )
                    throw new InvalidOperationException(String.Concat("The minimum bid allowed is R$ ", Int2Str(minOffer)));
                
                if ( isLock( args[0] ) )
                    throw new InvalidOperationException("The campaign has ended.");

                return Bid( (string)args[0],        // PP id
                            (string)args[1],        // member address
                            (BigInteger)args[2] );  // bid value
            }

            if (operation == "trade")
            {
                if ( args.Length != 4 )
                    throw new InvalidOperationException("Please provide the 4 arguments: your account address, the address of who you are transaction to, the quota value, and the amount of tokens.");

                if ( !Runtime.CheckWitness((string)args[0]) ) // --PENDING-- aqui o args[0] deve ser byte[]...
                    throw new InvalidOperationException("Only the owner of an account can exchange her/his asset.");

                if ( (args[1][0] != "A") || (args[1].Length == null) )
                    throw new InvalidOperationException("Provide a valid destiny address.");
                    
                if ( GetMemb(args[1], "FullName").Length != null )
                    throw new InvalidOperationException("The address you are transaction to must be a member too.");

                if ( (GetMemb(args[0], "Utility")) != (GetMemb(args[1], "Utility")) )
                    throw new InvalidOperationException( "Both members must belong to the same power utility cover area." );

                if ( (args[2] <= 0) & (args[3] <= 0) )
                    throw new InvalidOperationException("You're doing it wrong. To donate energy let ONLY the 4th argument empty. Otherwise, to donate tokens let ONLY the 3rd argument empty.");
                
                return Trade( (string)args[0],       // from address
                              (string)args[1],       // to address
                              (BigInteger)args[2],   // quota exchange
                              (BigInteger)args[3] ); // token price
            }

            if (operation == "power up")
            {
                if (args.Length != 4)
                    throw new InvalidOperationException("Please provide the 4 arguments: the PP capacity, the cost to build it up, the power utility name in which the PP will be installed, and the period to wait the new PP gets ready to operate.");

                if ( (args[3] == 0) || (args[3] < minTimeToMarket) )
                    throw new InvalidOperationException("The time to market must be a factual period.");

                return PowerUp( (BigInteger)args[0],    // capacity [MW]
                                (BigInteger)args[1],    // cost [R$]
                                (string)args[2],        // power utility name
                                (uint)args[3] );        // time to market
            }

            if (operation == "change")
            {
                if (args.Length != 2)
                    throw new InvalidOperationException("Please provide 2 arguments only. The first one must be the identification of the member (address) or the PP (id). The second one must be an array. It can be either the options about the data that will be changed, or an empty array to request the delete of something.");
                
                if ( (args[0][0] != "A") || args[0][0] != "P"  )
                    throw new InvalidOperationException("Provide a valid member address or PP ID.");
                    
                if ( (args[0][0] == "A") || (args[1].Length != 2) || (args[1].Length != 0) )
                    throw new InvalidOperationException("Provide valid arguments to update an address.");
                
                if ( (args[0][0] == "P") || (args[1].Length > 2) )
                    throw new InvalidOperationException("Provide valid arguments to update a PP subject.");
                
                if ( (args[1][0] in profile) & !(Runtime.CheckWitness(args[0])) )
                    throw new InvalidOperationException("Only the member can change its own personal data.");
                
                if ( (args[0][0] == "P") & (args[1].Length == 1) & !(args[1][0] is string) )
                    throw new InvalidOperationException("Provide a valid power utility name to be replaced by.");
                
                if ( (args[0][0] == "P") & (args[1].Length == 2) & !(Runtime.CheckWitness(args[1][0])) )
                    throw new InvalidOperationException("Only the member can change its bid.");
                
                if ( (args[0][0] == "P") & (args[1].Length == 2) & isLock( args[0] ) )
                    throw new InvalidOperationException("The campaign has ended.");
                
                return Change( (string)args[0],     // member address or PP id
                               (object[])args[1] ); // array with desired values --PENDING-- test length because of the problem of array of arrays...
            }
            
            // Administrative operations.
            if (operation == "admission result")
            {
                if ( args.Length != 1 )
                    throw new InvalidOperationException("Please provide the admission process ID.");
                
                if ( isLock( (string)args[0] ) )
                    throw new InvalidOperationException("There isn't a result yet.");
                
                return AdmissionResult( (string)args[0] ); // Referendum ID
            }
            
            if (operation == "change result")
            {
                if ( args.Length != 1 )
                    throw new InvalidOperationException("Please provide the change process ID.");
                
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
                
                ListOfPPs();
            }

            if (operation == "list of members")
            {
                if ( args.Length != 0 )
                    throw new InvalidOperationException("This function does not need attributes.");
                
                ListOfMembers();
            }
        }

        throw Warning();
        // return false;
    }

    return false;
}


//---------------------------------------------------------------------------------------------
// GROUP FUNCTIONS - The restrictions are made on the 'Main'.

// To request to join the group.
public static string Admission( string address, string fullName, string utility, params string[] list )
{
    string id = Ref( "Membership request_", String.Concat( fullName, utility ) );
    Membership( address, "Request for admission." );
    
    return id;
}

// To get information about something.
public static object Summary( string key, string opt = "" )     //--PENDING-- review dataset of each 'key' after the modifications made on the storage configuration.
{
    // If 'key' is an 'address' ==  member.
    if (key[0] == "A")
    {
        if ((opt == "") || (opt == "detailed"))
        {
            string[] brief = new string[] { GetMemb(key,"FullName"), GetMemb(key,"Utility"), GetMemb(key,"Quota"), GetMemb(key,"Tokens") };

            if (opt == "detailed")
            {
                GetContributeValue( key, list ); // list of PPs
            }
            return brief;
        }
        return GetMemb(key,opt);
    }

    // If 'key' is an 'id' with prefix 'P' == power plant.
    else if (key[0] == "P")
    {
        // The PP's crowdfunding had succeed and the PP is operating.
        if ( GetPP(key,"TotMembers").Length != 0 )
        {
            if ( (opt == "") || (opt == "detailed") )
            {
                string[] brief = new string[] { GetPP(key,"Capacity"), GetPP(key,"Cost"), GetPP(key,"Utility"), GetPP(key,"TotMembers") };
    
                if (opt == "detailed")
                {
                    GetContributeValue( key, list ); // list of members                    
                }
                return brief;
            }
            return GetPP(key,opt);
        }
        
        // The PP's crowdfunding may be succeed or not, and the PP is definitely not operating.
        else
        {
            if ( (opt == "") || (opt == "detailed") )
            {
                string[] brief = new string[] { GetCrowd(key,"Start Time"), GetCrowd(key,"End Time"), GetCrowd(key,"Total Amount"), GetCrowd(key,"Contributions"), GetCrowd(key,"Success") };

                if (opt == "detailed")
                {
                    foreach (int num in NumOfMemb())
                    {
                        string memberAddress = Storage.Get( String.Concat( "M", Int2Str(num) )).AsString();
                        BigInteger bid = GetBid(key, memberAddress).AsBigInteger();
                        
                        if ( bid != 0 )
                        {
                            Runtime.Notify( [memberAddress, bid] );
                        }
                    }
                }
                return brief;
            }
            return GetCrowd(key,opt); // sempre vai retornar byte[], a conversão final tem q ser feita de acordo com a opção escolhida para se ter o valor correto de número, texto ou boleano.
        }
    }

    // If 'key' is an 'id' with prefix 'R' == referendum process.
    else if (key[0] == "R")
    {
        if (opt == "")
        {
            return new string[] { GetRef(key,"Proposal"), GetRef(key,"Notes"), GetRef(key,"Cost"), GetRef(key,"Outcome") };
        }
        return GetRef(key,opt);
    }

    // Wrap-up the group information.
    else
    {
        return new string[] { PowGenLimits()[0], PowGenLimits()[1], NumOfPP(), NumOfMemb(), Name(), Symbol(), TotalSupply() };
    }
}

// To vote in a given ID process.
public static bool Vote( string id, string member, bool answer )
{
    // Increases the number of votes.
    BigInteger temp = GetRef(id,"Num of Votes").AsBigInteger();
    UpRef(id, "Num of Votes", temp++);

    if (answer)
    {
        // Increases the number of "trues".
        BigInteger temp = GetRef(id,"Count True").AsBigInteger();
        UpRef(id, "Count True", temp++);
    }

    // Publishes the vote.
    Ballot(id, member, answer);

    return answer;
}

// To make a bid in a new PP crowdfunding process.
public static bool Bid( string ICOid, string member, BigInteger bid )
{
    BigInteger target = GetPP(ICOid, "Cost").AsBigInteger();
    BigInteger funds = GetCrowd(ICOid, "Total Amount").AsBigInteger();
    
    if ( bid > (target - funds) )
        throw new InvalidOperationException( String.Concat(String.Concat("You offered more than the amount available (R$ ", Int2Str(target - funds) ), ",00). Bid again!" ));

    // WARNING!
    // All these steps are part of a crowdfunding process, not of a PP registration.
    
    // Increases the value gathered so far.
    UpCrowd(ICOid, "Total Amount", funds + bid);
    
    // Increases the number of contributions.
    BigInteger temp = GetCrowd(ICOid, "Contributions").AsBigInteger();
    UpCrowd(ICOid, "Contributions", temp++);
    
    // Tracks bid by member for each ICOid.
    BigInteger previous = Storage.Get( String.Concat(ICOid, member) ).AsBigInteger();
    Storage.Put( String.Concat(ICOid, member), previous + bid );
    Offer(ICOid, member, bid);
    
    return true;
    
    // If the hole fund process succeed, the money bid must be converted to percentage (bid/cost),
    // so it will be possible to define the quota and the SEB a member has to gain.
    // It is made on PowerUpResult(...).
}

// To update something on the ledger.
public object Change( string key, params object[] opts )
{
    // If 'key' is an 'address' ==  member.
    if (key[0] == "A")
    {
        // Only the member can change its own personal data.
        // To UPDATE, the params must be ['profile option', 'value'].
        if ( opts[1] is string )
        {
            UpMemb(key, opts[0], opts[1]);
            Update("Profile data.", key);
            return true;
        }
        
        // Any member can request the change of registration data of other member.
        // To UPDATE, the params must be ['register option', 'value'].
        if ( opts[1] is BigInteger )
        {
            string id = Ref( "Change register_", String.Concat( key, opts[0] ) );
            Process( id, "Request the change of registration data of a member." );
            return id;
        }
        
        // Any member can request to delete another member.
        // The 'opts.Length' is empty.
        string id = Ref("Delete member_", key);
        Process(id, "Request to dismiss a member.");
        return id;
    }
    
    // Otherwise, the 'key' is an 'id' with prefix 'P' == power plant.

    // Only the member can change its own bid.
    // To UPDATE, the params must be ['address', 'new bid value'].
    if ( opts.Length == 2 )
    {
        UpBid(key, opts[0], opts[1]);
        Update("Bid.", key);
        return true;
    }
    
    // Any member can request the change of the 'utility' a PP belongs to.
    // To UPDATE, the params must be ['new utility name'].
    if ( opts.Length == 1 )
    {
        string id = Ref( "Change utility_", String.Concat( key, opts[0] ) );
        Process( id, "Request the change of utility name of a PP." );
        return id;
    }

    // Any member can request to DELETE a PP.
    // The 'opts.Length' is empty.
    string id = Ref("Delete PP_", key);
    Process(id, "Request to delete a PP.");
    return id;
}

// The whole process to integrate a new PP on the group power generation.
public string PowerUp( BigInteger capacity, BigInteger cost, string utility, uint timeToMarket )
{
    string id = Ref( "New PP request_", String.Concat( capacity.ToString(), utility, timeToMarket.ToString() ), cost );
    Process( id, "Request to add a new PP." );
    return id;
}

// To allow the transfer of shares/tokens from someone to someone else (transactive energy indeed).
// The 'fromAddress' will exchange an amount of shares with 'toAddress' by a defined token price,
// i.e., while 'fromAddress' sends shares to 'toAddress', the 'toAddress' sends tokens to 'fromAddress'.
public bool Trade( string fromAddress, string toAddress, BigInteger exchange, BigInteger price )
{
    BigInteger[] toWallet = new BigInteger[];
    BigInteger[] fromWallet = new BigInteger[];
    
    // register = {"Quota", "Tokens"}
    foreach (string data in register)
    {
        fromWallet.append( GetMemb(fromAddress, data).AsBigInteger() );
        toWallet.append( GetMemb(toAddress, data).AsBigInteger() );
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
// SYSTEM FUNCTIONS

// A new PP will only distribute tokens and shares after a crowdfunding process succeed.
// All the exceptions were handle during the crowdfunding. It only needs to distribute the assets.
private static void Distribute( string toAddress, BigInteger quota, BigInteger tokens )
{
    BigInteger[] toWallet = new BigInteger[];

    // register = {"Quota", "Tokens"}
    foreach (string data in register)
    {
        toWallet.append( GetMemb(toAddress, data).AsBigInteger() );
    }
    
    UpMemb(toAddress, register[0], toWallet[0] + quota);
    UpMemb(toAddress, register[1], toWallet[1] + tokens);
    Transfer(null, toAddress, quota, tokens);
}

// To create a custom ID of a process based on its particular specifications.
private static string ID( params object[] args )
{
    string str = null;
    
    for (int k = 0; k < args.Length; k++)
    {
        int count = 0;
        for (int n = 0; n < Alpha().Length; n++)
        {
            if ( Alpha()[n] == ((string)args[k])[0] ) // args[k] is a 'string'
            {
                str = Rec( str, (string)args[k] );
                break;
            }
            count++;
        }
        
        if ( count == Alpha().Length ) // args[k] is a 'integer'
        {
            // Converts the related argument to string and concatenate.
            str = Rec( str, Int2Str( (int)args[k] ) );
        }
    }

    return str;
}

// To properly store a boolean variable.
private static string Bool2Str( bool val )
{
    if (val) return "1";
    return "0";
}

// To properly read a boolean from storage.
private static bool Str2Bool( byte[] val )
{
    if (val.AsString() == "1") return true;
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

// To affordably concatenate string variables.
private static string Rec(string start, string end)
{
    return String.Concat(start, end);
}

// To filter the relationship of members and PPs.
// Displays how much a member has contributed to a PP crowdfunding.
private static void GetContributeValue( string lookForID, string[] listOfIDs )
{
    // Gets values by each ID registered on the contract storage space.
    if ( lookForID[0] == "P" )
    {
        // Gets members' bid by a PP funding process.
        foreach (string memberAddress in listOfIDs)
        {
            BigInteger bid = GetBid(lookForID, memberAddress).AsBigInteger();
            
            if ( bid != 0 )
            {
                Runtime.Notify( [memberAddress, bid] );
            }
        }
    }
    else // lookForID[0] == "A"
    {
        // Gets PPs by a member investments.
        foreach (string PPid in listOfIDs)
        {
            BigInteger bid = GetBid(PPid, lookForID).AsBigInteger();
            
            if ( bid != 0 )
            {
                Runtime.Notify( [memberAddress, bid] );
            }
        }
    }
}

// To calculate the referendum result only once.
private static void CalcResult( string id )
{
    if ( GetRef(id, "Has Result").Length == 0 )
    {
        UpRef(id, "Has Result", 1);
    
        BigInteger totalOfVotes = GetRef(id, "Num of Votes").AsBigInteger();
        BigInteger totalOfTrues = GetRef(id, "Count True").AsBigInteger();
            
        if ( totalOfTrues > (totalOfVotes / 2) )
        {
            // Referendum has succeeded.
            UpRef(id, true);
        }
        
        // Otherwise, the "Outcome" remains as 'false'.
    }
}

// Actualy, it restricts a given operation to happen based on a timestamp.
// Before a given time frame, no one is allowed to continue the process.
// The monitoring of the time happens off-chain.
// Once the time stated is reached, any member can then resume the process.
private static bool isLock( string id )
{
    if (id[0] == "R")
    {
        uint endTime = GetRef(id, "End Time"); // --PENDING-- provavelmente vai dar erro de conversão!
    }
    uint endTime = GetCrowd(id, "End Time"); // --PENDING-- provavelmente vai dar erro de conversão!
    
    if (InvokeTime() <= endTime) return true;
    return false;
}


//---------------------------------------------------------------------------------------------
// ADMINISTRATIVE FUNCTIONS

// After a period of 'timeFrameRef' days a member should invoke this function to state the referendum process.
// An off-chain operation should handle this.

public static void AdmissionResult( string id )
{
    // Calculates the result
    CalcResult(id);
    
    if ( Str2Bool( GetRef(id, "Outcome") ) )
    {
        // Add a new member after approval from group members.
        Member( address, fullName, utility, 0, 0 );
        Membership( address, "Welcome on board!" );
    }

    Membership( address, "Not approved yet." );
}

public static void ChangeResult( string id, params string[] listOfMembers)
{
    string proposal = GetRef(id, "Proposal").AsString();
    
    if (proposal == "Change register_")
    {
        CalcResult(id);
        
        if ( Str2Bool( GetRef(id, "Outcome") ) )
        {
            Process(id, "Approved.");
            UpMemb(key, opts[0], opts[1]); // missing dependency --PENDING--
            Update("Registration data.", key);
        }
        
        Process(id, "Denied.");
    }
                
    if (proposal == "Delete member_")
    {
        CalcResult(id);
        
        if ( Str2Bool( GetRef(id, "Outcome") ) )
        {
            Process(id, "Approved.");
            BigInteger portion = GetMemb(key, "Quota").AsBigInteger();
            BigInteger give_out = portion/(NumOfMemb() - 1);
            
            foreach (string member in listOfMembers)
            {
                // In an infinitesimal period of time the group will be disbalanced
                // until the related member be completely deleted.
                // There is no side effect and it is better than iterate through each member.
                
                Distribute(member, give_out, 0);
            }
    
            DelMemb(key);
            Membership(key, "Goodbye.");
        }
    
        Process(id, "Denied.");
    }
    
    if (proposal == "Change utility_")
    {
        CalcResult(id);
        
        if ( Str2Bool( GetRef(id, "Outcome") ) )
        {
            Process(id, "Approved.");
            UpPP(key, opts[0]);  // missing dependency --PENDING--
            Update("Belonging of.", key);
        }

        Process(id, "Denied.");
    }
        
    if (proposal == "Delete PP_")
    {
        CalcResult(id);
        
        if ( Str2Bool( GetRef(id, "Outcome") ) )
        {
            Process(id, "Approved.");
            DelPP(key);
            Update("Deletion of.", key);
        }

        Process(id, "Denied.");
    }
}

public static object PowerUpResult( string id, string PPid = null, params string[] listOfFunders )
{
    // STEP 1 - After a 'timeFrameRef' waiting period.
    if (PPid == null)
    {
        if ( isLock(id) )
            throw new InvalidOperationException("There isn't a result about the new PP request yet.");
        
        // Evaluates the referendum result only once.
        if ( GetRef(id, "Has Result").Length == 0 )
        {
            CalcResult(id);
            
            if ( Str2Bool( GetRef(id, "Outcome") ) )
            {
                // Referendum has succeeded.
                
                // Adds a new PP.
                string notes = GetRef(id, "Notes"); // --PENDING--
                
                // separa os termos em Notes!           // --PENDING--
                String.Substring
                https://docs.microsoft.com/pt-br/dotnet/api/system.string.substring?view=netframework-4.8
                
                
                
                //            PP(capacity, cost, utility, time to market)
                string PPid = PP(notes[0], GetRef(id, "Cost"), notes[1], notes[2]);
                
                // Starts to raise money for it.
                CrowdFunding(PPid);
                Process(PPid, "Shut up and give me money!");
                return PPid;
            }
            
            // Otherwise...
            Process(id, "This PP was not approved yet. Let's wait a bit more.");
            return false;
        }
        
        return "This process are completed.";
    }
    
    // STEP 2 - After a 'timeFrameCrowd' waiting period.
    if ( isLock(PPid) )
        throw new InvalidOperationException("There isn't a result about the new PP crowdfunding yet.");
    
    // Evaluates the crowdfunding result only once.
    if ( GetCrowd(PPid, "Has Result").Length == 0 )
    {
        UpCrowd(PPid, "Has Result", 1);
        
        BigInteger target = GetPP(PPid, "Cost").AsBigInteger();
        BigInteger funding = GetCrowd(PPid, "Total Amount").AsBigInteger();
            
        // Starts or not the building of the new PP.
        if (funding == target)
        {
            // Crowdfunding has succeeded.
            UpCrowd(PPid, true);
            
            // Updates the number of investors.
            UpPP(PPid, "numOfFundMemb", listOfFunders.Length);
            
            Process(id, "New power plant on the way.");
            return true;
        }
        
        // Otherwise, the "Success" remains as 'false'.
        foreach (string funder in litsOfFunders)
        {
            Refund(PPid, funder);
        }
        
        Process(id, "Fundraising has failed.");
        return false;
    }
    
    // STEP 3 - After waiting for the time to market.
    
    // Calculates the date the new PP is planned to start to operate, that can always be updated until the deadline.
    // operationDate = ICO_endTime + PP_timeToMarket
    uint operationDate = GetCrowd(PPid, "End Time") + GetPP(PPid, "Time to Market");
    
    if ( InvokeTime() <= operationDate )
        throw new InvalidOperationException("The new PP is not ready to operate yet.");
    
    // Evaluates the construction only once.
    if ( GetPP(PPid, "Has Started").Length == 0 )
    {
        // When the PP is ready to operate, it's time to distribute tokens and shares.
        
        
            
        // Increases the total power supply of the group.
        BigInteger capOfPP = GetPP(PPid, "Capacity").AsBigInteger();
        BigInteger capOfGroup = TotalSupply() + capOfPP;
        Storage.Put("TotalSupply", capOfGroup);
    
        // How much the new Power Plant takes part on the group total power supply.
        BigInteger sharesOfPP = capOfPP/capOfGroup;
        
        foreach (string funder in litsOfFunders)
        {
            // Gets the member contribution.
            BigInteger grant = GetBid(PPid, funder).AsBigInteger();
            
            // How much a member has from the new PP's capacity.
            BigInteger tokens = grant/capOfPP; // --PENDING-- rever unidades e cálculos (R$/MW ou kW/MW ?)
            
            // How much a member has from the updated total power supply.
            BigInteger quota = tokens * sharesOfPP; // --PENDING-- rever unidades e cálculos (R$/MW ou kW/MW ?)
    
            Distribute(funder, quota, tokens);
        }
    
        Process(PPid, "A new power plant is now operating.");
        return true;
    }
    
    return "There is nothing more to be done.";
}

// To display the IDs of each PP to be later used on other functions.
private static void ListOfPPs()
{
    for (int num = 1; num < NumOfPP()+1; num++)
    {
        string PPid = Storage.Get( String.Concat( "P", Int2Str(num) )).AsString();
        Runtime.Notify( PPid );
    }
}

// To display the address of each member to be later used on other functions.
private static void ListOfMembers()
{
    for (int num = 1; num < NumOfMemb()+1; num++)
    {
        string memberAddress = Storage.Get( String.Concat( "M", Int2Str(num) )).AsString();
        Runtime.Notify( memberAddress );
    }
}


//---------------------------------------------------------------------------------------------
// METHODS FOR MEMBERS
// --> create
private static void Member( string address, string fullName, string utility, BigInteger quota, BigInteger tokens )
{
    Storage.Put( String.Concat( address, "FullName" ), fullName );
    Storage.Put( String.Concat( address, "Utility" ), utility );
    Storage.Put( String.Concat( address, "Quota" ), quota );
    Storage.Put( String.Concat( address, "Tokens" ), tokens );

    // Increases the total number of members.
    BigInteger temp = NumOfMemb() + 1;
    Storage.Put("NumOfMemb", temp);
    
    // Stores the address of each member.
    Storage.Put( String.Concat( "M", temp.ToString() ), address );
}

// --> read
private static byte[] GetMemb( string address, string opt )
{
    return Storage.Get( String.Concat( address, opt ) );
}

// --> update
// Detailed restrictions to update 'profile' or 'register' data are set
// on the function 'Change'. Here this feature is handled by polymorphism.
private static void UpMemb( string address, string opt, string val )
{
    // Don't invoke Put if value is unchanged.
    string orig = GetMemb(address, opt).AsString();
    if (orig == val) return;
     
    // Use Delete rather than Put if the new value is empty.
    if (val.Length == 0)
    {
       DelMemb(address, opt);
    }
    else
    {
       Storage.Put( String.Concat( address, opt ), val );
    }
}

private static void UpMemb( string address, string opt, BigInteger val )
{
    // Don't invoke Put if value is unchanged.
    BigInteger orig = GetMemb(address, opt).AsBigInteger();
    if (orig == val) return;
     
    // Use Delete rather than Put if the new value is zero.
    if (val == 0)
    {
       DelMemb(address, opt);
    }
    else
    {
       Storage.Put( String.Concat( address, opt ), val );
    }
}

// --> delete
private static void DelMemb( string address, string opt = "" )
{
    // If a member exits the group.
    if (opt == "")
    {
        Storage.Delete( String.Concat( address, "FullName" ) );
        Storage.Delete( String.Concat( address, "Utility" ) );
        Storage.Delete( String.Concat( address, "Quota" ) );
        Storage.Delete( String.Concat( address, "Tokens" ) );
        
        // Decreases the total number of members.
        BigInteger temp = NumOfMemb() - 1;
        Storage.Put("NumOfMemb", temp);
        
        // Wipe off the address of the member.
        Storage.Delete( String.Concat( "M", ? ), address ); // -- PENDING --
    }

    // To support an economic action for the update method.
    Storage.Delete( String.Concat( address, opt ) );
}

//---------------------------------------------------------------------------------------------
// METHODS FOR POWER PLANTS
// --> create
private static string PP( string capacity, BigInteger cost, string utility, uint timeToMarket )
{
    string id = ID("P", capacity, cost, utility);
    if ( GetPP(id, "Capacity").Length != 0 )
    {
        Process(id, "This power plant already exists. Use the method UpPP to change its registering data.");
        return;
    }
    
    Storage.Put( String.Concat( id, "Capacity" ), capacity );
    Storage.Put( String.Concat( id, "Cost" ), cost );
    Storage.Put( String.Concat( id, "Utility" ), utility );
    Storage.Put( String.Concat( id, "Time to Market" ), timeToMarket );
    // Storage.Put( String.Concat( id, "Num of Fund Memb" ), 0 ); // Expensive to create with null value. Just state it out!
    // Storage.Put( String.Concat( id, "Has Started" ), 0 ); // Expensive to create with null value. Just state it out!

    // Increases the total number of power plant units.
    BigInteger temp = NumOfPP() + 1;
    Storage.Put("NumOfPP", temp);
    
    // Stores the ID of each PP.
    Storage.Put( String.Concat( "P", Int2Str(temp) ), id );

    Process(id, "New PP created.")
    return id;
}

// --> read
private static byte[] GetPP( string id, string opt )
{
    return Storage.Get( String.Concat( id, opt ) );
}

// --> update
// The 'Utility', the 'HasStarted', and the 'Time To Market' are the only options that can be changed.
// However, the 'Utility' can be changed anytime, the 'HasStarted' can be changed only once, while the 'Time to Market' is restricted by its deadline of start operation date.
// To update the other options, delete the current PP and create a new one.
private static void UpPP( string id, string opt, object val )
{
    if (opt == "Utility")
    {
        // Don't invoke Put if value is unchanged.
        string orig = GetPP(id, "Utility").AsString();
        if (orig == val) return;
        
        // Do nothing if the new value is empty.
        if (val.Length == 0) return;
        
        // else
        Storage.Put( String.Concat( id, "Utility" ), val );
        // And must 'update' each member 'utility' field as well.
        // 'Utility' should be a pointer and similar to 'Member' dataset.
        // This was not implemented!
    }
    
    if (opt == "Has Started")
    {
        // Don't invoke Put if value is unchanged.
        string orig = GetPP(id, "Has Started").AsBigInteger();
        if (orig == val) return;
        
        // Do nothing if the new value is empty.
        if (val.Length == 0) return;
        
        // else
        Storage.Put( String.Concat( id, "Has Started" ), val );
    }
    
    if (opt == "Time to Market")
    {
        if ( InvokeTime() > ( GetCrowd(PPid, "End Time") + GetPP(PPid, "Time to Market") ) )
            throw new InvalidOperationException("The time has passed by. You can no longer postpone it.");
        
        // Don't invoke Put if value is unchanged.
        string orig = GetPP(id, "Time to Market").BigInteger();
        if (orig == val) return;
        
        // Do nothing if the new value is empty.
        if (val == 0) return;
        
        // else
        Storage.Put( String.Concat( id, "Time to Market" ), val );
    }
}

// --> delete
private static void DelPP( string id )
{
    Storage.Delete( String.Concat( id, "Capacity" ) );
    Storage.Delete( String.Concat( id, "Cost" ) );
    Storage.Delete( String.Concat( id, "Utility" ) );
    if ( GetPP(id, "Num of Fund Memb") != 0 ) Storage.Delete( String.Concat( id, "Num of Fund Memb" ) );

    // Decreases the total number of power plant units.
    BigInteger temp = NumOfPP() - 1;
    Storage.Put("NumOfPP", temp);

    // Decreases the total power supply of power plants.
    BigInteger temp = TotalSupply() - GetPP(id, "Capacity").AsBigInteger();
    Storage.Put("TotalSupply", temp);
    
    // Wipe off the id of the PP.
    Storage.Delete( String.Concat( "P", ? ), id ); // -- PENDING --
}

//---------------------------------------------------------------------------------------------
// METHODS FOR REFERENDUMS
// --> create
private static string Ref( string proposal, string notes, int cost = 0 )
{
    string id = ID("R", proposal, notes, cost);
    if ( GetRef(id, "Proposal").Length != 0 )
    {
        Process(id, "This referendum already exists. Use the method UpRef to change its registering data, or just start a new referendum process.");
        return "-";
    }
    
    Storage.Put( String.Concat( id, "Proposal" ), proposal );
    Storage.Put( String.Concat( id, "Notes" ), notes );
    Storage.Put( String.Concat( id, "Cost" ), cost );
    // Storage.Put( String.Concat( id, "Money Raised" ), 0 ); // Expensive to create with null value. Just state it out!
    // Storage.Put( String.Concat( id, "Num of Votes"), 0 );   // Expensive to create with null value. Just state it out!
    // Storage.Put( String.Concat( id, "Count True"), 0 );    // Expensive to create with null value. Just state it out!
    Storage.Put( String.Concat( id, "Outcome" ), Bool2Str(false) );
    // Storage.Put( String.Concat( id, "Has Result"), 0 );    // Expensive to create with null value. Just state it out!
    Storage.Put( String.Concat( id, "Start Time" ), InvokeTime() );
    Storage.Put( String.Concat( id, "End Time" ), InvokeTime() + timeFrameRef );

    Process(id, "The referendum process has started.");
    return id;
}

// The function to vote on a referendum is declared above because it is public.

// --> read
private static byte[] GetRef( string id, string opt )       // retorna byte[] OU object? --PENDING--
{
    return Storage.Get( String.Concat( id, opt ) );
}

// --> update
// It is only possible to internally change the 'MoneyRaised', the 'NumOfVotes', the 'CountTrue', the 'HasResult' and the 'Outcome'.
private static void UpRef( string id, string opt, BigInteger val )
{
    if ((opt == "Num of Votes") || (opt == "Money Raised") || (opt == "Count True") || (opt == "Has Result") )
    {
        // Don't invoke Put if value is unchanged.
        BigInteger orig = GetRef(id, opt).AsBigInteger();
        if (orig == val) return;
         
        // Delete the storage if the new value is zero.
        if (val == 0) return Storage.Delete( String.Concat(id, opt) );
        
        // else
        Storage.Put( String.Concat( id, opt ), val );
    }
}

private static void UpRef( string id, bool val )
{
    // Don't invoke Put if value is unchanged.
    string orig = Str2Bool( GetRef(id, "Outcome") );
    if ( orig == Bool2Str(val) ) return;
        
    // else
    Storage.Put( String.Concat( id, "Outcome" ), Bool2Str(val) );
}

// --> delete
// A referendum process remains forever... and ever.


//---------------------------------------------------------------------------------------------
// METHODS TO FINANCE A NEW POWER PLANT
// --> create
private static void CrowdFunding( string ICOid )
{
    Storage.Put( String.Concat( ICOid, "Start Time" ), InvokeTime() );
    Storage.Put( String.Concat( ICOid, "End Time" ), InvokeTime() + timeFrameCrowd );
    // Storage.Put( String.Concat( ICOid, "Total Amount" ), 0 );   // Expensive to create with null value. Just state it out!
    // Storage.Put( String.Concat( ICOid, "Contributions" ), 0 ); // Expensive to create with null value. Just state it out!
    Storage.Put( String.Concat( ICOid, "Success" ), Bool2Str(false) );
    // Storage.Put( String.Concat( ICOid, "Has Result" ), 0 );  // Expensive to create with null value. Just state it out!
}

// The function to bid on a crowdfunding is declared above because it is public.

// --> read
private static BigInteger GetBid( string ICOid, string member )
{
    return Storage.Get( String.Concat( ICOid, member ) );
}

private static object GetCrowd( string ICOid, string opt )              // retorna byte[] OU object? --PENDING--
{
    return Storage.Get( String.Concat( ICOid, opt ) );
}

// --> update
private static bool UpBid( string ICOid, string member, BigInteger bid ) // --PENDING-- return... Preciso retornar alguma coisa?
{
    // Don't invoke Put if value is unchanged.
    BigInteger orig = GetBid(ICOid, member).AsBigInteger();
    if (orig == bid) return;
     
    // Delete the storage if the new value is zero.
    if (bid == 0) return Refund(ICOid, member);
    Storage.Delete( String.Concat(id, opt) );
    
    // else
    Storage.Put( String.Concat( ICOid, member ), bid );
    
    
    // Update other crowd values! --PENDING--
    
    return true;
}

// Only the 'Total Amount', 'Contributions', 'HasResult' and 'Success' can be updated.
private static void UpCrowd( string ICOid, string opt, BigInteger val )
{
    if ( (opt == "Total Amount") || (opt == "Contributions") || (opt == "Has Result") )
    {
        // Don't invoke Put if value is unchanged.
        BigInteger orig = GetCrowd(ICOid, opt).AsBigInteger();
        if (orig == val) return;
         
        // Delete the storage if the new value is zero.
        if (val == 0) return DelCrowd(ICOid, opt);
        
        // else
        Storage.Put( String.Concat( ICOid, opt ), val );
    }
}

private static void UpCrowd( string ICOid, bool val )
{
    // Don't invoke Put if value is unchanged.
    string orig = Str2Bool( GetCrowd(ICOid, "Success") );
    if ( orig == Bool2Str(val) ) return;
        
    // else
    Storage.Put( String.Concat( ICOid, "Success" ), Bool2Str(val) );
}

// --> delete
private static void Refund( string ICOid, string member )
{
    // Deletes the member's offer.
    BigInteger grant = GetBid(ICOid, member);
    Storage.Delete( String.Concat( ICOid, member ) );
    
    // Decreases the total amount of funds
    BigInteger funds = GetCrowd(ICOid, "Total Amount");
    UpCrowd(PPi, "Total Amount", funds - grant);

    // Decreases the total number of contributions.
    BigInteger contributions = GetCrowd(ICOid, "Contributions");
    UpCrowd(ICOid, "Contributions", contributions--);
    
    // Sends the money back to the member.
    Trade(ICOid, member, 0, grant); // --PENDING-- aqui é SEB ou REAIS?
    Refund(member, grant);
}

// Only the 'Total Amount' and 'Contributions' can be "deleted"
// because the failure of a crowdfunding must be preserved.
// Actually it is only used to "store" null values cheaply.
private static void DelCrowd( string ICOid, string opt )    // --PENDING-- Why not keep this information?
{
    if ( (opt == "Total Amount") || (opt == "Contributions") )
    {
        Storage.Delete( String.Concat( ICOid, opt ) );
    }
}


//---------------------------------------------------------------------------------------------
https://github.com/neo-project/examples/blob/master/csharp/NEP5/NEP5.cs




// TO TEST
//---------------------------------------------------------------------------------------------

Neo.Header.GetTimestamp         // Get the timestamp of the block
Neo.Storage.GetContext          // [New] Get the current store context
Neo.Contract.GetStorageContext  // [New] Get the storage context of the contract

// get sender script hash
Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
TransactionOutput[] reference = tx.GetReferences();
reference[0].ScriptHash;

// to update ID and comparation of IDs operations
    string temp1 = String.Concat( "Hello world", "outra coisa" );
    string temp2 = String.Concat( "P", temp1);

    byte[] result2 = temp2.AsByteArray();

    Runtime.Notify( result2.AsString()[0] == 'P' ); // comparação entre string's, mas "P" não funciona...
    
    
    
    
    
    
    
    
    
// TO DO
//---------------------------------------------------------------------------------------------
// It must be an offline operation! From an offline monitoring, any Neo user could continue the process invoking the function again. However, it will only work if the user has a membership ID.

// CRIAR UMA OPERAÇÃO DA WALLET QUE POSSA FAZER ISSO! Exemplos de wallet?

// ---------------

// To unlock some operations to keep going.
// It automatically invokes this smart contract to continue a function from where it has been locked.
private static void Unlock(func equivalencia?) // como passar o comando para uma função específica?
{
    // Blockchain... Execute ( Owner(), function );
    Blockchain.GetAccount( Owner() ); // Get an account based on the scripthash of the contract
    Blockchain.GetAccount( Owner() ); // Get contract content based on contract hash
    Transaction.GetHash; //	Get Hash for the current transaction
    Transaction.GetAttributes; //	Query all properties of the current transaction
    Account.GetScriptHash; //	Get the script hash of the contract account
    Contract.GetScript; //	Get the scripthash of the contract
    
}
