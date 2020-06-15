// REMAINING QUESTIONS AND PERSONAL NOTES

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


// Qual a diferença mesmo para o Runtime.Notify()? Aparentemente o custo e como a notificação aparece... Ainda não sei qual é melhor para se seguir em uma API.

// public byte[] caller = será passado como parâmetro! pubKey (or address?) of the user whose interacts with the smart contract.
// fonte: https://docs.neo.org/en-us/sc/reference/api/System.html


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

(string)args[2] // esse 'string' é uma declaração ou um condicional?


// Posso usar isso?
// https://docs.microsoft.com/pt-br/dotnet/csharp/programming-guide/inside-a-program/coding-conventions#linq-queries


Ballot( id, caller, answer ); // a ordem de publicação pode facilitar a busca na blockchain? Na blockchain ou no RPC?


// timestamp --> https://docs.neo.org/developerguide/en/articles/blockchain/block.html
// https://docs.microsoft.com/pt-br/dotnet/api/system.datetime.today?view=netframework-4.8

// WAITING TIME!
// ...do nothing
// Smart Contract Example - Lock (Lock Contract)
// https://docs.neo.org/en-us/sc/tutorial/Lock.html


// gather votes on-chain! -- procurar por publicações do tipo 'event' com a referida 'id'
// Blockchain.GetTransaction( ? )
// Blockchain.Transaction.Type( ? )
// Um voto é do tipo 'PublishTransaction'?
// Tem algum identificador único para esse tipo de função e para essa transação?
// https://docs.neo.org/en-us/sc/reference/fw/dotnet/neo/Transaction/Type.html


// Updating methods
// TESTAR ISSO!
// fonte: https://docs.neo.org/en-us/sc/reference/fw/dotnet/neo/Storage.html


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