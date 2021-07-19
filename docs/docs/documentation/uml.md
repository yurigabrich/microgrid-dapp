---
layout: default
title: UML Diagram
parent: Documentation
nav_order: 1
---

# The UML Diagram

The following diagram presents a general view of the structure and communication between the objects concepts and properties of the considered blockchain system.
Therefore, the MTEsm has four agents (the general person, the general user, the member, and the group consensus) with different access type to the smart contract information.
In the other hand, the blockchain has only the blockchain consensus as a agent responsible for what will be appended or not in the ledger.

Moreover, the dashed lines highlight different aspects of the Dapp:

> **A** - The core concepts of the blockchain technology: the ledger and the consensus mechanism.

> **B** - The different attributes and interactions of an ordinary person (reader) and a general user.
And how the former becomes the latter through the blockchain registration platform.

> **C** - The difference between the general user and the member of a group.
Now, the registration process is defined by a group referendum and limits the acces to the group's private space and personalized smart contract operations.

> **D** - The MTEsm environment indeed.
Besides the sets of operations, some attributes are split to show how similar aspects are presented on different steps of the system, such as the waiting period.


However, only the most relevant relationships between agents and entities are considered, for instance:

> - The associations between *ordinary person*, *general user*, and *member* highlights the two consensus methods,
how the access to the ledger information changes, and the different access someone may have to the MTEsm environment.

> - The *Referendum Process* is a generalisation of any request to change a value in the MTEsm private space,
with a unique identifier and particular waiting time for the group consensus.

> - The relationships are not exhaustive.
For example, the registering data of power plants and members have quite similar attributes,
so, the method to update one might work to the other, and hence, only one kind of this relation was considered on the diagram.

> - The associations with external interfaces (off-chain) are easily identifiable too.
The way any trading process must proceed is detached from the check-out step that formalizes the energy trade (on-chain).
And the waiting period required for a given operation to happen is counted away of how the blockchain performs the analysis to allow or not it.

> - The composite aggregation (black diamonds) states the unique relationship between objects.
For instance, each crowdfunding process is solely related to the construction of a new power plant that represents the unique way to create new cryptocurrencies (SEBs).

<br> 

![uml](https://raw.githubusercontent.com/yurigabrich/microgrid-dapp/master/pseudo-code/imgs/uml.png)
*The UML class diagram of the Dapp proposed. [(GABRICH, 2019, appendix C)](/microgrid-dapp/docs/references)*
