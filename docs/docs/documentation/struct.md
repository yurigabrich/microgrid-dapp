---
layout: default
title: Code Structure
parent: Documentation
nav_order: 2
---

# The code structure


Several components compose a whole smart contract but only a fraction of it is available for interaction by the blockchain user, as highlighted in the figure below.
Also, the figure presents a common code organization for the development of smart contracts,
where the functions are divided by their attributions and expected output.
In summary, users may interact with the MTEsm through the operations available under the main interface (solid box),
while the remaining functions (dashed boxes) support all the operations needs.

<br>

![code structure](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/struct.png)
*The structure of the MTEsm code. [(GABRICH, 2019, figure 5)](microgrid-dapp/docs/references)*

<br>

Therefore, the MTEsm execution starts when a user call one of the available functions with its parameters.
As described in the figure below, some functions are dependent on the permission of the user, which can be a member or not of the shareable consumption group.
However, if the operation requested does not output an error, there are still a lot of steps before the end of the activity.
This is represented by the blocks with statements "Executes the respective operation." and "Executes the respective restricted operation."
Then, the remaining operation continues in the following figures, according to one of the functions chosen:
[admission](./#admission),
[summary](./#summary),
[vote](./#vote),
[bid](./#bid),
[change](./#change),
[power-up](./#power-up), or
[trade](./#trade).

<br>

![main interface](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/main-interface.png)
*The flowchart of the MTEsm main interface. [(GABRICH, 2019, figure 6)](microgrid-dapp/docs/references)*

<br>

In syntesis, call a function is make a transaction with a smart contract, in this case with the MTEsm address.
Therefore, to get the expected output it is important to enter the right parameters of each function.
In this project, some possible errors were identified and are well handled by the MTEsm.
Although the errors are considered as a general term in the following figures for simplicity,
its description is available for further analysis in Appendix D [(GABRICH, 2019)](microgrid-dapp/docs/references).

## Admission

The first function a user may interact with to request to join the group.
After a members consensus for a period of 30 days (default value), the process result is ready.
If the user is approved to be a new member of the group, 
she/he gains access to other functions only belonging to the group private environment.

<br>

![figure 7](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/fig7.png)
*The pathway to be accepted in a group's private environment. Functions (b) and (c) complements the whole process presented on (a). [(GABRICH, 2019, figure 7)](microgrid-dapp/docs/references)*

<br>

## Summary

This function has a special behaviour since its output denpends on who has requested it, i.e.,
a user has limited access to several kinds of the group information, such as a member balance of tokens or quota.
However, everyone can get information of the power plants and referendums statuses, for instance.

<br>

![figure 8](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/fig8.png)
*The process to get information from the group. Function (b) complements the whole process presented on (a), while (c) is one of the respective steps of (b), that continues in the next figure. [(GABRICH, 2019, figure 8)](microgrid-dapp/docs/references)*

![figure 9](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/fig9.png)
*Flowcharts (a) and (b) complement the respective steps presented in the figure above. [(GABRICH, 2019, figure 9)](microgrid-dapp/docs/references)*

<br>
 
## Vote

The function to vote on every referendum-like process.
Any group decision always depends on each member vote, so each member has the same power vote, i.e., 1 member is equal to 1 vote.
However, a blockchain code requires a financial thinking to handle with the execution expenses.
Thefore, only positive votes are counted and absence votes are weighted as negative answers when evaluating the result.

<br>

![figure 10](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/fig10.png)
*A general voting procedure called by a member. Function (b) complements the whole process presented on (a). Note that the return argument of (b) means the success of the ballot, and not the vote answer. [(GABRICH, 2019, figure 10)](microgrid-dapp/docs/references)*

<br>
 
## Bid

Likewise the vote function, the bid function aims to state a member position front of a group decision but now the subject is to determine
how much the member will contribute to finance a new power plant.
Restrictions for the transaction also follow the Brazilian legislation since a member cannot have a portion of energy from a generation outside the member power network.
In the end, all information for this kind of crowdfunding keep recorded forever even if it fails or a cancellation happens.

<br>

![figure 11](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/fig11.png)
*The process of a new power plant crowdfunding. Function (b) complements the whole process presented on (a). [(GABRICH, 2019, figure 11)](microgrid-dapp/docs/references)*

<br>

## Change

The function to update the information already registered in the group private space.
However, there are two sets of information available.
One is concerned to the member own data, for instance, the profile data or a bid on a crowdfunding.
This kind of information can be changed without a group agreement, therefore, it is not required to wait for an entire referendum process to change the data.
The other set is always a referendum-like process that requires members vote and period for discussions.

A particular change of the data is the option to delete an information.
Then, the deletion of a member data will redistribute the quotas for the remaining members proportionally to each one.
But the deletion of a power plant will not impact on the members registers since their quotas is a percentage rate of the group power capacity,
not of the single value of a power plant capacity.

<br>

![figure 12](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/fig12.png)
*The full updating process of several information. Further details in the next figures. [(GABRICH, 2019, figure 12)](microgrid-dapp/docs/references)*

![figure 13](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/fig13.png)
*Function (a) complements the process presented in the figure before, while (b) and (c) complement the final steps of (a). Although both have different outcomes, they share a common pattern such as one option to update data without group consensus. [(GABRICH, 2019, figure 13)](microgrid-dapp/docs/references)*

![figure 14](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/fig14.png)
*The step (b) intermediates the referendum succeess in the function (a) and the final steps reproduced in the next figure. [(GABRICH, 2019, figure 14)](microgrid-dapp/docs/references)*

![figure 15](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/fig15.png)
*The end of the update process accordingly with each proposal identified in the figure above, item (b). [(GABRICH, 2019, figure 15)](microgrid-dapp/docs/references)*

<br>

## Power-up

This function aims to provide the tools to manage any power plant registering data.
Any member can request to add a new power plant, however, until a power generation start, a referendum process must succeed firstly.
This process is different because, beyond the traditional waiting period of 30 days of a ballot, there are two additional waiting times, one for the crowdfunding (pre-defined to 60 days), and other for the power plant construction until its gets ready to operate (pre-defined to 30 days too).
For the crowdfunding case, the \gls{mtesm} registers how much each member is up to pay to fund a given amount of power, but the bids coordination and payment have to be made in off-chain platforms.
If funding succeed, the power plant construction period starts.
The interaction with real-time planning is subtle and this variable can be updated before its deadline.
This waiting time is required to avoid unbalanced distribution of energy among members.
In other words, to update the share fraction of the whole group based on the new power plant auction, the \gls{mtesm} must wait the date the new power plant is ready to operate.
Moreover, this function also supports the group crypto-currency market (\gls{SEB}) since every time a new power plant is approved, some tokens are proportionally created.
However, their distribution happens once the whole new power plant implementation process ends.
Therefore, independently of the number of beneficiaries, the last step of the \emph{power-up} operation is a transaction that distributes tokens all at once, making the returning values always account for 100\% of the new power implemented.

<br>

![figure 16](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/fig16.png)
*.... [(GABRICH, 2019, figure 16)](microgrid-dapp/docs/references)*

![figure 17](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/fig17.png)
*.... [(GABRICH, 2019, figure 17)](microgrid-dapp/docs/references)*

![figure 18](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/fig18.png)
*.... [(GABRICH, 2019, figure 18)](microgrid-dapp/docs/references)*

figure 16
The process to increase the group power capacity (1).
Functions (b) and (c) complement the whole process presented on (a).

figure 17
The process to increase the group power capacity (2).
The steps (a) and (b) complement the initia operations in Fig 16c.

figure 18
The process... (3)
The analysis of the PP operation status complements the last step in Fig 16c.

<br>
 
## Trade

A member can exchange quotas with anyone else by a price agreed upon themselves (an off-chain communication process).
Important to remember that this value is represented by means of the group's token created for this purpose, the \gls{SEB}.
However, the principle of donation is preserved depending on how null values are transacted.
If the objective is to donate energy, an exchange of quotas is made with a zero token price.
On the other hand, a donation of \glspl{SEB} has null values of quotas.
To keep the group transparency about the quota shares, the \gls{mtesm} displays a notification every time the function is triggered.

<br>

![figure 19](https://github.com/yurigabrich/microgrid-dapp/blob/master/pseudo-code/imgs/fig19.png)
*.... [(GABRICH, 2019, figure 19)](microgrid-dapp/docs/references)*

figure 19
The trade agreement process between members.
Function (b) complements the process (a).

<br>
