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
public static BigInteger NumOfPP() => Storage.Get("NumOfPP").AsBigInteger();

// The total number of members.
public static BigInteger NumOfMemb() => Storage.Get("NumOfMemb").AsBigInteger();

// The total power supply at the group, i.e., sum of PP's capacity.
public static BigInteger TotalSupply() => Storage.Get("TotalSupply").AsBigInteger();

// Token settings.
public static string Name() => "Sharing Electricity in Brazil";
public static string Symbol() => "SEB";
public static byte Decimals() => 3;                                                         // {0, 5000}
public static byte[] Owner() => ExecutionEngine.ExecutingScriptHash;                        // aka GetReceiver() -- this smart contract
public static string[] SupportedStandards() => new string[] { "NEP-5", "NEP-7", "NEP-10" };

// Member's dataset.
private static string[] profile => new string[] {"FullName", "Utility"};
private static string[] register => new string[] {"Quota", "Tokens"};

// New Power Plant crowdfunding settings (ICO).
private const ulong factor = 1000;              // decided by Decimals() --PENDING--
private const ulong neo_decimals = 100000000;   // --PENDING--
private const byte minOffer = 1;


//---------------------------------------------------------------------------------------------
// THE MAIN INTERFACE
















//---------------------------------------------------------------------------------------------
// FUNCTIONS - The restrictions are made on the 'Main'.

// To request to join the group.  (must avoid double requests!!!) <---------------- --PENDING--
public static void Admission( string address, string fullName, string utility )
{
    string id = Ref( "Membership request_", String.Concat( fullName, utility ) );
    
    // Must lock the contract for a while!!! --PENDING--
    
    if ( Str2Bool( GetRef(id, "Outcome") ) )
    {
        // Add a new member after approval from group members.
        Member( address, fullName, utility, 0, 0 );
        Membership( address, "Welcome on board!" );
        return;
    }
    Membership( address, "Not approved yet." );
}

// To vote in a given ID process.
public static void Vote( string id, string member, bool answer )
{
    // Increase the number of votes.
    BigInteger temp = GetRef(id,"NumOfVotes").AsBigInteger();
    UpRef(id, "NumOfVotes", temp++);

    if (answer)
    {
        // Increase the number of "trues".
        BigInteger temp = GetRef(id,"CountTrue").AsBigInteger();
        UpRef(id, "CountTrue", temp++);
    }

    // Publish the answer.
    Ballot(id, member, answer);
}

// To make a bid in a new PP crowdfunding process.
public static bool Bid( string PPid, string member, BigInteger bid )
{
    // Check parameters.    ------------------------------ The restrictions must be made on the 'Main'.
    if ( (PPid[0] != "P") || (PPid.Length == 0) )
        throw new InvalidOperationException( "Provide a valid PP address." );
    if ( (GetMemb(member, "FullName") == null) || (member.Length == 0) )
        throw new InvalidOperationException( "Only members can bid." );
    if ( bid <= 0 ) return false;
        throw new InvalidOperationException( "Stop being a jerk." );
    
    
    BigInteger target = GetPP(PPid, "Cost").AsBigInteger();
    BigInteger funds = GetCrowd(PPid, "TotalAmount").AsBigInteger();
    
    if ( bid > target - funds )
        throw new InvalidOperationException( "You offered more than the amount requested ({0}). Bid again!".format( target - funds ) );

    // WARNING!
    // All these steps are part of a crowdfunding process, not of a PP registration.
    
    // Increases the value gathered so far.
    UpCrowd(PPid, "TotalAmount", funds + bid);
    
    // Increases the number of contributions.
    BigInteger temp = GetCrowd(PPid, "Contributions").AsBigInteger();
    UpCrowd(PPid, "Contributions", temp++);
    
    // Tracks bid by member for each PPid.
    BigInteger previous = Storage.Get( String.Concat(PPid, member) ).AsBigInteger();
    Storage.Put( String.Concat(PPid, member), previous + bid );
    Offer(PPid, member, bid);
    return true;
    
    // If the hole fund process succeed, the money bid must be converted to percentage (bid/cost),
    // so it will be possible to define the quota and the SEB a member has to gain.
}

// To get information about something.
public static object Summary( string key, string opt = "" )
{
    // If 'key' is an 'address' ==  member.
    if (key[0] == "A")
    {
        if ((opt == "") || (opt == "detailed"))
        {
            string[] brief = new string[] { GetMemb(key,"FullName"), GetMemb(key,"Utility"), GetMemb(key,"Quota"), GetMemb(key,"Tokens") };

            if (opt == "detailed")
            {
                string[] PowerPlantsByMember = GetContributeValue( key, listOfPPs() );
                return brief + PowerPlantsByMember; // wrong concatenation method --PENDING--
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
                    string[] MembersByPowerPlant = GetContributeValue( key, listOfMembers() );
                    return brief + MembersByPowerPlant; // wrong concatenation method --PENDING--
                }
                return brief;
            }
            return GetPP(key,opt);
        }
        
        // The PP's crowdfunding may be succeed or not and the PP is definitely not operating.
        else
        {
            if ( (opt == "") || (opt == "detailed") )
            {
                string[] brief = new string[] { GetCrowd(key,"StartTime"), GetCrowd(key,"EndTime"), GetCrowd(key,"TotalAmount"), GetCrowd(key,"Contributions"), GetCrowd(key,"Success") };
    
                if (opt == "detailed")
                {
                    string[][] PowerPlantBids = new string[][];
                    
                    for each member in Members() // to be implemented {[Member, quota]} ? HOW? --PENDING--
                    {
                        BigInteger bid = GetBid(key, member).AsBigInteger();
                        if ( bid != 0 ) PowerPlantBids.append( [member, bid] );
                    }
                    
                    return brief + PowerPlantBids; // wrong concatenation method --PENDING--
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

// To update something on the ledger.
private void Change( string key, params object[] opts )
{
    // If 'key' is an 'address' ==  member.
    if (key[0] == "A")
    {
        // Only the member can change its own personal data.
        // To UPDATE, the params must be ['profile option', 'value'].
        if ( (Runtime.CheckWitness(key)) & (opts[1] is string) )
        {
            UpMemb(key, opts[0], opts[1]);
            Update("Profile data.", key);
        }
        
        // Any member can request the change of registration data of other member
        // To UPDATE, the params must be ['register option', 'value'].
        if ( opts[1] is BigInteger )
        {
            string id = Ref( "Change register_", String.Concat( key, opts[0] ) );
    
            // Must lock the contract for a while!!! --PENDING--
            
            if ( Str2Bool( GetRef(id, "Outcome") ) )
            {
                Process(id, "Approved.");
                UpMemb(key, opts[0], opts[1]);
                Update("Registration data.", key);
            }
        Process(id, "Denied.");
        }
        
        // Any member can request to delete another member
        if ( opts.Length == 0 )
        {
            string id = Ref( "Delete member_", "Distribute the shares and delete the tokens." );
            
            // Must lock the contract for a while!!! --PENDING--
            
            if ( Str2Bool( GetRef(id, "Outcome") ) )
            {
                Process(id, "Approved.");
                BigInteger portion = GetMemb(key, "Quota").AsBigInteger();
                BigInteger give_out = portion/(NumOfMemb() - 1);
                
                // implement the loop for distribution --PENDING--

                DelMemb(key);
                Membership(key, "Goodbye.");
            }
            Process(id, "Denied.");
        }
    }
    
    // If 'key' is an 'id' with prefix 'P' == power plant.
    if (key[0] == "P")
    {
        // Any member can request the change of the 'utility' a PP belongs to.
        if ( opts.Length != 0 )
        {
            string id = Ref( "Change utility_", String.Concat( key, opts[0] ) );
    
            // Must lock the contract for a while!!! --PENDING--
            
            if ( Str2Bool( GetRef(id, "Outcome") ) )
            {
                Process(id, "Approved.");
                UpPP(key, opts[0]);
                Update("Belonging of.", key);
            }
            Process(id, "Denied.");
        }

        // Any member can request to DELETE a PP.
        string id = Ref( "Delete PP_", String.Concat( key, opts[0] ) );
    
        // Must lock the contract for a while!!! --PENDING--
        
        if ( Str2Bool( GetRef(id, "Outcome") ) )
        {
            Process(id, "Approved.");
            DelPP(key);
            Update("Deletion of.", key);
        }
        Process(id, "Denied.");
    }
}

// To allow the transfer of shares/tokens from someone to someone else (transactive energy indeed).
// The 'fromAddress' will exchange an amount of shares with 'toAddress' by a defined token price,
// i.e., while 'fromAddress' sends shares to 'toAddress', the 'toAddress' sends tokens to 'fromAddress'.
private bool Trade( string fromAddress, string toAddress, BigInteger exchange, BigInteger price )
{
    BigInteger[] toWallet = new BigInteger[];
    
    if ( !Runtime.CheckWitness(fromAddress) ) // essas condições tem que estar no main! -- PENDING --
        throw new InvalidOperationException( "Only the owner of an account can exchange her/his asset." );
    if ( fromAddress and toAddress not a member ) // essas condições tem que estar no main! -- PENDING --
        throw new InvalidOperationException( "Only members can trade. Join us!" ); // acho q isso já estará restrito em algum momento.
    if ( GetMemb(fromAddress, "Utility") != GetMemb(toAddress, "Utility") ) // essas condições tem que estar no main! -- PENDING --
        throw new InvalidOperationException( "Both members must belong to the same utility cover area." );
    
    BigInteger[] fromWallet = new BigInteger[];
    
    // register = {"Quota", "Tokens"}
    foreach (string)data in register
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

// A new PP will just distribute tokens and shares after a crowdfunding process succeed.
// All the exceptions were handle during the crowdfunding. It only needs to distribute the assets.
private static void Distribute( string toAddress, BigInteger quota, BigInteger tokens )
{
    BigInteger[] toWallet = new BigInteger[];

    // register = {"Quota", "Tokens"}
    foreach (string)data in register
    {
        toWallet.append( GetMemb(toAddress, data).AsBigInteger() );
    }
    
    UpMemb(toAddress, register[0], toWallet[0] + quota);
    UpMemb(toAddress, register[1], toWallet[1] + tokens);
    Transfer(null, toAddress, quota, tokens);
}

// To create a custom ID of a process based on its particular specifications.
private static string ID( object arg1, object arg2, object arg3, object arg4 )
{
    // 'object' solves the problem but miss the information.

    string temp1 = String.Concat(arg1, arg2);
    string temp2 = String.Concat(arg3, arg4);
    return String.Concat(temp1, temp2);
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

// To filter the relationship of members and PPs.
private static string[] GetContributeValue(string lookForID, string[] listOfIDs)
{
    //
    string[] equivList = new string[];
    
    // Gets values by each ID registered on the contract storage space.
    if ( lookForID[0] == "P" )
    {
        // Gets members by a PP.
        foreach (string key in listOfIDs)
        {
            BigInteger GetBid(lookForID, key).AsBigInteger();
            if ( temp != 0 ) equivList.append(temp);
        }
    }
    else
    {
        // Gets PPs by a member.
        foreach (string key in listOfIDs)
        {
            BigInteger GetBid(key, lookForID).AsBigInteger();
            if ( temp != 0 ) equivList.append(temp);
        }
    }
    
    return equivList;
}

// To get the IDs of each PP.
private static string[] listOfPPs()
{
    string[] listPPs = new string[];
    
    foreach num in NumOfPP()
    {
        string PP = Storage.Get( String.Concat( "P", num.ToString() ) ); // --PENDING--
        listMembers.append(PP);
    }
    
    return listPPs;
}

// To get the address of each member.
private static string[] listOfMembers()
{
    string[] listMembers = new string[];
    
    foreach num in NumOfMemb()
    {
        string member = Storage.Get( String.Concat( "M", num.ToString() ) ); // --PENDING--
        listMembers.append(member);
    }
    
    return listMembers;
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
private static void PP( string capacity, BigInteger cost, string utility )
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
    // Storage.Put( String.Concat( id, "NumOfFundMemb" ), 0 ); // Expensive to create with null value. Just state it out!

    // Increases the total number of power plant units.
    BigInteger temp = NumOfPP() + 1;
    Storage.Put("NumOfPP", temp);
    
    // Stores the ID of each PP.
    Storage.Put( String.Concat( "P", temp.ToString() ), id );
    
    Process(id, "New PP created.")
}

// --> read
private static byte[] GetPP( string id, string opt )
{
    return Storage.Get( String.Concat( id, opt ) );
}

// --> update
// The 'utility' is the only option that can be changed.
// To update the other options, delete the current PP and create a new one.
private static void UpPP( string id, string val )
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
}

// --> delete
private static void DelPP( string id )
{
    Storage.Delete( String.Concat( id, "Capacity" ) );
    Storage.Delete( String.Concat( id, "Cost" ) );
    Storage.Delete( String.Concat( id, "Utility" ) );
    if ( GetPP(id, "NumOfFundMemb") != 0 ) Storage.Delete( String.Concat( id, "NumOfFundMemb" ) );

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
    // Storage.Put( String.Concat( id, "MoneyRaised" ), 0 ); // Expensive to create with null value. Just state it out!
    // Storage.Put( String.Concat( id, "NumOfVotes"), 0 );   // Expensive to create with null value. Just state it out!
    // Storage.Put( String.Concat( id, "CountTrue"), 0 );    // Expensive to create with null value. Just state it out!
    Storage.Put( String.Concat( id, "Outcome" ), Bool2Str(false) );

    Process(id, "The referendum process has started.");
    return id;
}

// The function to vote on a referendum is declared above, because it is public.

// --> read
private static byte[] GetRef( string id, string opt )
{
    return Storage.Get( String.Concat( id, opt ) );
}

// --> update
// It is only possible to change the 'MoneyRaised', the 'NumOfVotes', the 'CountTrue' and the 'Outcome'.
private static void UpRef( string id, string opt, BigInteger val )
{
    if ((opt == "NumOfVotes") || (opt == "MoneyRaised") || (opt == "CountTrue"))
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
// METHODS TO EVALUATE A NEW POWER PLANT (aka an ICO of a NFT)
// --> create
private static void CrowdFunding( string PPid, int startTime, int endTime)
{
    Storage.Put( String.Concat( PPid, "StartTime" ), startTime ); // --PENDING--
    Storage.Put( String.Concat( PPid, "EndTime" ), endTime ); // --PENDING--
    // Storage.Put( String.Concat( PPid, "TotalAmount" ), 0 );   // Expensive to create with null value. Just state it out!
    // Storage.Put( String.Concat( PPid, "Contributions" ), 0 ); // Expensive to create with null value. Just state it out!
    Storage.Put( String.Concat( PPid, "Success" ), Bool2Str(false) );
}

// The function to bid on a crowdfunding is declared above, because it is public.

// --> read
private static BigInteger GetBid( string PPid, string member )
{
    return Storage.Get( String.Concat( PPid, member ) );
}

private static object GetCrowd( string PPid, string opt )
{
    return Storage.Get( String.Concat( PPid, opt ) );
}

// --> update
private static bool UpBid( string PPid, string member, BigInteger bid )
{
    // Don't invoke Put if value is unchanged.
    BigInteger orig = GetBid(PPid, member).AsBigInteger();
    if (orig == bid) return;
     
    // Delete the storage if the new value is zero.
    if (bid == 0) return Refund(PPid, member);                               Storage.Delete( String.Concat(id, opt) );
    
    // else
    Storage.Put( String.Concat( PPid, member ), bid );
    return true;
}

// Only the 'Total Amount', 'Contributions' and 'Success' can be updated.
private static void UpCrowd( string PPid, string opt, BigInteger val )
{
    if ( (opt == "TotalAmount") || (opt == "Contributions") )
    {
        // Don't invoke Put if value is unchanged.
        BigInteger orig = GetCrowd(PPid, opt).AsBigInteger();
        if (orig == val) return;
         
        // Delete the storage if the new value is zero.
        if (val == 0) return DelCrowd(PPid, opt);
        
        // else
        Storage.Put( String.Concat( PPid, opt ), val );
    }
}

private static void UpCrowd( string PPid, bool val )
{
    // Don't invoke Put if value is unchanged.
    string orig = Str2Bool( GetCrowd(PPid, "Success") );
    if ( orig == Bool2Str(val) ) return;
        
    // else
    Storage.Put( String.Concat( PPid, "Success" ), Bool2Str(val) );
}

// --> delete
private static void Refund( string PPid, string member )
{
    // Deletes the member's offer.
    BigInteger grant = GetBid(PPid, member);
    Storage.Delete( String.Concat( PPid, member ) );
    
    // Decreases the total amount of funds
    BigInteger funds = GetCrowd(PPid, "TotalAmount");
    UpCrowd(PPi, "TotalAmount", funds - grant);

    // Decreases the total number of contributions.
    BigInteger contributions = GetCrowd(PPid, "Contributions");
    UpCrowd(PPid, "Contributions", contributions--);
    
    // Sends the money back to the member.
    Trade(PPid, member, 0, grant);
    Refund(member, grant);
}

// Only the 'Total Amount' and 'Contributions' can be "deleted"
// because the failure of a crowdfunding must be preserved.
// Actually it is only used to "store" null values cheaply.
private static void DelCrowd( string PPid, string opt )
{
    if ( (opt == "TotalAmount") || (opt == "Contributions") )
    {
        Storage.Delete( String.Concat( PPid, opt ) );
    }
}


//---------------------------------------------------------------------------------------------


https://github.com/neo-project/examples/blob/master/csharp/NEP5/NEP5.cs


private void WhereItWillBePlaced(?)
{
    // Must lock the contract for a while!!! --PENDING--

    if ( Str2Bool( GetRef(id, "Outcome") ) )
    {
        // Starts the crowdfunding...
        // Starts to raise money after approval from group members for a new PP. --PENDING-- ICO!
        // if ( (.TODAY() > start_time) && (.TODAY() < end_time) ) // Crowdfunding is still available
        ...
        // Must lock the contract for a while!!! --PENDING--

        // If crowdfunding succeeds.
        if funding ok:
        {
            // Update the number of fund members database
            BigInteger numOfFundMemb = ...; // --PENDING--
            UpPP(id, "numOfFundMemb", numOfFundMemb);
            Process(id, "New power plant on the way.");

            // Must lock the contract for a while!!! --PENDING--
            ...
            
            // When the PP starts to operate, it's time to distribute tokens and shares.
            
            // Increases the total power supply of the group.
            BigInteger capOfPP = GetPP(PPid, "Capacity").AsBigInteger();
            BigInteger capOfGroup = TotalSupply() + capOfPP;
            Storage.Put("TotalSupply", capOfGroup);

            // What the presence of the PP account for on the group.
            BigInteger sharesOfPP = capOfPP/capOfGroup;

            // Gets a list of funders of the respective PP.
            string[] litsOfFunders = GetContributeValue( PPid, listOfMembers() );
            
            foreach funder in litsOfFunders
            {
                BigInteger grant = GetBid(PPid, funder).AsBigInteger();
                BigInteger tokens = grant/capOfPP; // --PENDING-- rever unidades e cálculos
                BigInteger quota = tokens * sharesOfPP; // --PENDING-- rever unidades e cálculos

                Distribute(funder, quota, tokens);
                Transfer(null, funder, quota, tokens);
            }

            Process(id, "A new power plant is now operating.")
        }

        // If crowdfunding fails.
        if (contributions < target)
        {
            Refund(sender, contribute_value);
            Process(id, "Fundraising has failed.");
        }
        
    }
    
    // If referendum for a new PP fails.
    Process(id, "Let's wait a bit more.");
}
