//---------------------------------------------------------------------------------------------
// EVENTS

[DisplayName("transaction")]
public static event Action<byte[], byte[], BigInteger> Transfer;
[DisplayName("membership")]
public static event Action<string, string> Membership;
[DisplayName("process")]
public static event Action<string, string> Process;
[DisplayName("ballot")]
public static event Action<string, string, bool> Ballot;
[DisplayName("change")]
public static event Action<string, string> Update;
[DisplayName("refund")]
public static event Action<string, BigInteger> Refund;


//---------------------------------------------------------------------------------------------
// GLOBAL VARIABLES

// Power limits of the distributed generation category defined by Brazilian law (0MW até 5MW).
public static int[] PowGenLimits() => new int[] {0, 5000000};

// The total number of power plant units.
public static BigInteger NumOfPP() => Storage.Get("NumOfPP").AsBigInteger();

// The total number of members.
public static BigInteger NumOfMemb() => Storage.Get("NumOfMemb").AsBigInteger();

// The amount of tokens already created.
public static BigInteger TotalSupply() => Storage.Get("TotalSupply").AsBigInteger();

// Member's dataset.
private static string[] profile => new string[] {"FullName", "Utility"};
private static string[] register => new string[] {"Quota", "Tokens"};


//---------------------------------------------------------------------------------------------
// THE MAIN INTERFACE
















//---------------------------------------------------------------------------------------------
// FUNCTIONS - The restrictions are made on the 'Main'.

// To request to join the group.  (must avoid double requests!!!) <----------------
public static void Admission( string address, string fullName, string utility )
{
    string id = Ref( "Membership request_", String.Concat( fullName, utility ) );
    
    // Must lock the contract for a while!!! --PENDING--
    
    if ( GetRef(id, "Outcome") )
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

// To get information about something.
public static object Summary(string key, string opt = "")
{
    // If 'key' is an 'address' ==  member.
    if (key[0] == "A")
    {
        if ((opt == "") || (opt == "detailed"))
        {
            string[] brief = new string[] { GetMemb(key,"FullName"), GetMemb(key,"Utility"), GetMemb(key,"Quota"), GetMemb(key,"Tokens") };

            if (opt == "detailed")
            {
                string[] PowerPlantsByMember = PPMem(key); // to be implemented {[PP, quota]} ? HOW? --PENDING--
                return brief + PowerPlantsByMember; // wrong concatenation method --PENDING--
            }
            return brief;
        }        
        return GetMemb(key,opt);
    }

    // If 'key' is an 'id' with prefix 'P' == power plant.
    else if (key[0] == "P")
    {
        if ((opt == "") || (opt == "detailed"))
        {
            string[] brief = new string[] { GetPP(key,"Capacity"), GetPP(key,"Cost"), GetPP(key,"Utility"), GetPP(key,"TotMembers") };

            if (opt == "detailed")
            {
                string[] MembersByPowerPlant = MemPP(key); // to be implemented {[Member, quota]} ? HOW? --PENDING--
                return brief + MembersByPowerPlant; // wrong concatenation method --PENDING--
            }
            return brief;
        }
        return GetPP(key,opt);
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
        return new string[] { PowGenLimits()[0], PowGenLimits()[1], NumOfPP(), NumOfMemb(), TotalSupply() };
    }
}

// To create a custom ID of a process based on its particular specifications.
private static string ID(object arg1, object arg2, object arg3, object arg4)
{
    // 'object' solves the problem but miss the information.

    string temp1 = String.Concat(arg1, arg2);
    string temp2 = String.Concat(arg3, arg4);
    return String.Concat(temp1, temp2);
}

// To properly storage a boolean variable.
private static string ConvBool(bool val)
{
    if (val) return "1";
    return "0";
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
            
            if ( GetRef(id, "Outcome") )
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
            
            if ( GetRef(id, "Outcome") )
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
            
            if ( GetRef(id, "Outcome") )
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
        
        if ( GetRef(id, "Outcome") )
        {
            Process(id, "Approved.");
            DelPP(key);
            Update("Deletion of.", key);
        }
        Process(id, "Denied.");
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

    // Increase the total number of members.
    BigInteger temp = NumOfMemb() + 1;
    Storage.Put("NumOfMemb", temp);
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
        
        // Decrease the total number of members.
        BigInteger temp = NumOfMemb() - 1;
        Storage.Put("NumOfMemb", temp);
    }

    // To support an economic action for the update method.
    Storage.Delete( String.Concat( address, opt ) );
}

//---------------------------------------------------------------------------------------------
// METHODS FOR POWER PLANTS
// --> create
private static void PP( string capacity, BigInteger cost, string utility, BigInteger numOfFundMemb )
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
    Storage.Put( String.Concat( id, "NumOfFundMemb" ), numOfFundMemb );

    // Increase the total number of power plant units
    BigInteger temp = NumOfPP() + 1;
    Storage.Put("NumOfPP", temp);
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
    Storage.Delete( String.Concat( id, "NumOfFundMemb" ) );

    // Decrease the total number of power plant units
    BigInteger temp = NumOfPP() - 1;
    Storage.Put("NumOfPP", temp);

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
    Storage.Put( String.Concat( id, "Outcome" ), ConvBool(false) );

    Process(id, "The referendum process has started.");
    return id;
}

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
    string orig = GetRef(id, "Outcome");
    if ( orig == ConvBool(val) ) return;
        
    // else
    Storage.Put( String.Concat( id, "Outcome" ), ConvBool(val) );
}

// --> delete
// A referendum process remains forever... and ever.















//---------------------------------------------------------------------------------------------


//---------------------------------------------------------------------------------------------
// METHODS FOR BIDS -- refazer! -- pois isso é um processo do ICO, já tem pronto!
// --> create
private static void Bid( string id, BigInteger amount, string member )
{
    // check if it is still NEEDED!!!
    // ...

    // sum the amount to the money in raise
    BigInteger temp = GetRef(id,"MoneyRaised");
    UpRef(id, "MoneyRaised", temp+amount);

    // save the member contribution for each fund process
    Storage.Put( Storage.Concat( "Bid", id, member ), amount );
}

// --> read
public static bool GetBid( string id, string member )
{
    return Storage.Get( Storage.Concat( "Bid", id, member ) ).AsString(); // testar de novo por causa do string, bool e BigInteger!
}

// --> update
private static void UpBid( string id, string member, BigInteger val )
{
    // check if it is still POSSIBLE!!!
    // ...

    // Don't invoke Put if value is unchanged. 
    BigInteger orig = GetBid(id, member);
    if (orig == val) return;
     
    // If the new value is empty or zero.
    if (val.Length == 0) || (val == 0)
    {
        DelBid(id, member);
    }
    
    // else
    Storage.Put( Storage.Concat( "Bid", id, member ), val );
}

// --> delete
private static void DelBid( string id )
{
    Storage.Delete( Storage.Concat( "Bid", id, member ) );
}
