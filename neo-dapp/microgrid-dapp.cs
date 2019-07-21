// TO UNDERSTAND!
// https://docs.neo.org/en-us/sc/deploy-invoke.html
// RELER!!!
// https://docs.neo.org/en-us/sc/write/basics.html

// 00x0 vem daqui?
// https://docs.neo.org/en-us/sc/Parameter.html
// ou daqui?
// https://docs.neo.org/en-us/sc/reference/fw/dotnet/neo/TransactionAttribute/Usage.html
// E o q q isso tem a ver?
// https://docs.neo.org/en-us/sc/reference/fw/dotnet/neo/TriggerType.html

using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Neo.SmartContract                                 // N1
{
    /**************
     * Main Class *                                         // N1.C1
     *************/
    public class GGM : Framework.SmartContract
    {
        /****************
         * Basic Events *
         ****************/
        // Qual a diferença mesmo para o Runtime.Notify()? Aparentemente o custo e como a notificação aparece... Ainda não sei qual é melhor para se seguir em uma API.
        [DisplayName("transaction")]
        public static event Action<byte[], byte[], BigInteger> Transfer; // --> OK!
        [DisplayName("membership")]
        public static event Action<string, string> Membership; // --> OK!
        [DisplayName("process")]
        public static event Action<byte[], string> Process; // --> OK!
        [DisplayName("ballot")]
        public static event Action<byte[], byte[], bool> Ballot; // --> OK!
        [DisplayName("change")]
        public static event Action<string, byte[]> Update; // --> OK!
        
        /*******************************
         * Global Variables Definition * // --> OK!
         ******************************/
        // public byte[] caller = será passado como parâmetro! pubKey (or address?) of the user whose interacts with the smart contract.
        // fonte: https://docs.neo.org/en-us/sc/reference/api/System.html
        
        /*****************
         * Main Function *
         *****************/
        public static Object Main ( string operation, params object[] args )
        {
            if ( Runtime.Trigger == TriggerType.Verification ) // only once at deploy? Verification type is not deployed, so how it works? How can I access it?
            {
                // Check if someone has already initially the SC... But I can have several "equal" SC... Several deploys of the same SC has the same address? Yes!
                if ( Member.Get() == null ) // condição para o deploy e não para o verification...
                {
                    if (args.Length != 2) return false;
                    Member( caller, args[0], args[1], 100, 0 );
                    return "New GGM blockchain initiated.";
                }
                return false;
            }
            else if ( Runtime.Trigger == TriggerType.Application ) // only works after deploy --> appcall : invoke method!
            {
                // general operations
                if (operation == "summary") return Summary();
                if (operation == "admission")
                {
                    if (args.Length != 2) return false;
                    return Admission( (string)args[0],   // caller address
                                      (string)args[1],   // fullName
                                      (string)args[2] ); // utility
                                      // esse 'string' é uma declaração ou um condicional?
                }
                
                // private operations
                if ( Storage.Get( caller ) != null ) // if caller is a Member
                {
                    if (operation == "change") return Change( (string)args[0], (params object[])args? );
                    if (operation == "power")
                    {
                        if (args.Length != 3) return false;
                        return Plant( (string)args[0],       // capacity
                                      (BigInteger)args[1],   // cost
                                      (string)args[2]);      // description
                    }
                    if (operation == "trade")
                    {
                        if (args.Length != 3) return false;
                        return Trade( (string)args[0],       // Trade Agreement Statement
                                      (byte[])caller,        // from
                                      (byte[])args[1],       // to
                                      (BigInteger)args[2]);  // exchanged
                    }
                    if (operation == "vote")
                    {
                        if (args.Length != 2) return false;
                        return Vote( (byte[])args[0],   // referendum_id
                                     (bool)args[1] );   // answer
                    }
                }
                return false;
            }
            return false;
        }
        
        /********************
        /* Public Functions *
        /*******************/
        public void Summary() // --> OK!
        {
            // PowGenLimits : [ Int, Int ] // limits of the group power generation
            // NumPP : Int // total number of power plant units
            // NumM : Int // total number of members
            // QuanT : Float // the amount of tokens
            // shares : Map // the distribution of tokens [pubKey, quota]
            
            // Posso usar isso?
            // https://docs.microsoft.com/pt-br/dotnet/csharp/programming-guide/inside-a-program/coding-conventions#linq-queries
        }
        
        public void Admission( string address, string fullName, string utility ) // --> OK!
        {
            if Referendum( fullName, address.ToBigInteger(), utility )
            {
                // Add a new member after approval from group members.
                Member( address, fullName, utility, 0, 0 );
                Membership( address, "Welcome on board!" );
            }
            Membership( address, "Not approved yet." );
        }
        
        /*********************
        /* Private Functions *
        /********************/
        private void Change( string id, params object[] opts ) // --> OK!
        {
            // ALTERAR TUDO AQUI!
            if ( id.Length == 20 ) // check pubKey length
            {
                // Only the member can change its own personal data.
                // UPDATE registration data (Name or utility)
                if ( opts[1] is string ) & ( Runtime.CheckWitness(id) )
                {
                    Member.Update( id, opts[1], opts[0]);
                    Update( "Personal data.", id );
                }
                
                // Any member can request the modification of "specs" data of other member
                // UPDATE power data (tokens or shares)
                if ( opts[1] is BigInteger )
                {
                    Switch( id, opts[1], (new string[]){opts[0]} );
                }
                
                // DELETE A MEMBER
                if ( opts.Length == 1 ) // condição ruim...
                {
                    // Erase values from the 'id' and distribute the amount for the remaining members
                    Switch( id, -1*opts[0], (new string[]){"shares", "tokens"} );
                    
                    // 2) Definitely delete
                    Member.Delete( id );
                    Membership( id, "Goodbye." );
                }
            }
            
            if ( id.Length == 33 ) // check id length
            {
                // Update power plant data
                
                // DELETE A POWER PLANT
                
            }
        }
        
        private void Plant( string capacity, BigInteger cost, string description ) // --> OK!
        {
            var answer = Referendum( capacity, cost, description );
            if answer:
                // Register a new power plant after approval from group members.
                NPP( answer.ID() ); // acho q vai dar errado!!!
                Process( answer.ID(), "New power plant on the way." );
                
            Process( answer.ID(), "Let's wait a bit more." );
        }
        
        private bool Trade( string statement, byte[] from, byte[] to, BigInteger exchange ) // transactive energy
        {
            var walletFrom = Member.BalanceOf( from );
            var walletTo = Member.BalanceOf( to );
            
            if ( walletFrom < exchange ) return false;
            
            Member.Update( from, walletFrom - exchange );
            Member.Update( to, walletTo + exchange );
            Runtime.Notify( statement ); // Do I really need this?
            Transfer( from, to, exchange );
            return true;
        }
        
        private bool Vote( byte[] id, bool answer ) // --> OK!
        {
            Ballot( id, caller, answer ); // a ordem de publicação pode facilitar a busca na blockchain? Na blockchain ou no RPC?
            return true;
        }
        
        private void Switch( byte[] id, BigInteger value, string[] choices )
        {
            // Get the address of every member.
            BigInteger TotMemb = Member.Get();
            
            // Update values for 'shares', 'tokens', or both.
            for opt in choices
            {
                // Set basic operations by each option type.
                if ( opt == "shares" )
                {
                    BigInteger portion = Member.SharesOf( id );
                    if (value < 0) string[] msgs = ["No more group shares.", "shares saved."];
                    string[] msgs = ["shares distributed.", "shares received."];
                }
                else if ( opt == "tokens" )
                {
                    BigInteger portion = Member.BalanceOf( id );
                    if (value < 0) string[] msgs = ["No more group tokens.", "tokens saved."];
                    string[] msgs = ["tokens distributed.", "tokens distribution."];
                }
                else continue // in case a wrong 'opt' may appear
                
                BigInteger give_out = portion/( TotMemb.Length - 1 );
                
                if ( Referendum( opt, portion, "All remaining members will receive " + give_out.ToString() ) )
                { // after approval
                    BigInteger diff = portion + value;
                    Member.Update( id, diff, opt );
                    Update( msgs[0], id );
                    
                    for memb in TotMemb
                    {
                        if ( opt == "shares" )
                        {
                            Member.Update( memb, give_out, opt );
                            Update( msgs[1], memb );
                        } else { // "tokens"
                            Trade( msgs[1], id, memb.ToScriptHash(), give_out );
                            // It already has an event notification.
                        }
                    }
                }
            }
        }
    }

    /*******************
     * Private Classes *                                    // N1.Cx
     ******************/
    class Referendum // --> OK!
    {
        // Constructor
        public bool Referendum ( string proposal, BigInteger cost, string notes )
        {
            byte[] id = ID( proposal, cost, notes );
            
            StorageMap referendum = Storage.CurrentContext.CreateMap( id );
            Process(id, "It has started.")
            
            return Consensus( ...7dias..., id );
        }
        
        // Function
        private bool Consensus ( byte[] id, DateTime timeframe ) // alterar no UML
        {
            var ListOfMembers = Storage.GetAll()...
            uint temp = ListOfMembers.Length; // because the number of members can change during the wait time
            
            // uint today = Header.Timestamp ?
            while (DateTime.Today < timeframe)
            {
                // timestamp --> https://docs.neo.org/developerguide/en/articles/blockchain/block.html
                // https://docs.microsoft.com/pt-br/dotnet/api/system.datetime.today?view=netframework-4.8
                
                // WAITING TIME!
                // ...do nothing
                // Smart Contract Example - Lock (Lock Contract)
                // https://docs.neo.org/en-us/sc/tutorial/Lock.html
                
                // Isso não seria igual a:
                // if (DateTime.Today > timeframe) ... do ... as operações abaixo?
            }
            
            // gather votes on-chain! -- procurar por publicações do tipo 'event' com a referida 'id'
            // Blockchain.GetTransaction( ? )
            // Blockchain.Transaction.Type( ? )
            // Um voto é do tipo 'PublishTransaction'?
            // Tem algum identificador único para esse tipo de função e para essa transação?
            // https://docs.neo.org/en-us/sc/reference/fw/dotnet/neo/Transaction/Type.html
            
            // Checks the number of votes.
            if (temp == gathered_votes)
            {
                // todos votaram
            } else {
                // complementa com votos nulos e publica esse fato
            }
            
            // Sums the votes.
            int sum_votes = 0;
            for v in gathered_votes
            {
                if (v)
                {
                    sum_votes++;
                }
            }
            
            // Evaluates the votes.
            string msg = "denied";
            if (sum_votes > temp/2)
            {
                msg = "approved";
            }
            
            // Saves and publishes the vote process.
            Storage.Put( id, msg );
            Process(id, msg);
        }
        
        // Method
        public byte[] ID( string A, BigInteger B, string C )
        {
            return (string.Format("{0} | {1} | {2}", A, B, C)).toScriptHash();
        }
    }
    
    private void Member( string address, string fullName, string utility, BigInteger quota, BigInteger tokens ) // --> OK!
    {
        StorageMap member = Storage.CurrentContext.CreateMap( address );
        member.Put( "fullName", fullName );
        member.Put( "utility", utility );
        member.Put( "quota", quota );
        member.Put( "tokens", tokens );
    }
    
    class Member // Depends on 'Referendum' answer but not inherit its functions. // --> OK!
    // com classe a nomenclatura fica confusa... Repensar esta funcionalidade!
    {
        
        // Printing methods
        public string Get( string who = "all" )
        {
            if (who == "all")
            {
                 ... Map.Keys() ou .Values() ? ;
            }
            
            return Storage.Get(Storage.CurrentContext, who.Values()).AsString();
            
            // Runtime.Notify( member[pubKey] ); // colocar junto a chamada
            // qual é mais barato? notify ou return?
        }
        
        public string BalanceOf( string who )
        {
            return Storage.Get(Storage.CurrentContext, who+"\x00"+"tokens").AsBigInteger();
        }
        
        public string SharesOf( string who )
        {
            return Storage.Get(Storage.CurrentContext, who+"\x00"+"quota").AsBigInteger();
        }
        
        // Updating methods
        // TESTAR ISSO!
        // fonte: https://docs.neo.org/en-us/sc/reference/fw/dotnet/neo/Storage.html
        public void Update( string who, string with, string key)
        {
            // 'key' must be either 'fullName' or 'utility'
            Storage.Put( Storage.CurrentContext, who+"\x00"+key, with );
        }
        
        public void Update( string who, BigInteger with, string key = "tokens" )
        {
            // 'key' must be either 'quota' or 'tokens' (by default)
            Storage.Put( Storage.CurrentContext, who+"\x00"+key, with );
        }
        
        // Removing method
        public void Delete( string who )
        {
            Storage.Delete( Storage.CurrentContext, who );
        }
    }
    
    class NPP // Depends on 'Referendum' answer but not inherit its functions. // --> OK!
    {
        // Constructor
        public NPP( byte[] id )
        {
            StorageMap power_plant = Storage.CurrentContext.CreateMap( id );
        }
        
        public void Status() // Isso está completamente errado!
        {
            var answers = Answers();
            var result = false;
            var count = 0;
            
            for a in answers
            {
                if a : count++;
            }
            // answers.Sum() // Posso fazer isso?
            
            if ( count > answers.Length/2 ) : result = true;
            
            Runtime.Notify( result );
        }
        
        // Removing methods
        public void Delete( byte[] id)
        {
            Storage.Delete( id ); // ?? não vai dar merda?
        }
    }
}
