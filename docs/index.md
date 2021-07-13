---
layout: default
title: Home
nav_order: 1
description: "MTEsm"
permalink: /
---

# Microgrid Transactive Energy Smart Contract
{: .fs-9 }

The beginning of a blockchain application to pave the way for Transactive Energy at Brazilian micro/mini-grids.
{: .fs-6 .fw-300 }

[Get started now](https://yurigabrich.github.io/microgrid-dapp/docs/example){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 } [View it on GitHub](https://github.com/yurigabrich/microgrid-dapp){: .btn .fs-5 .mb-4 .mb-md-0 }

---

## Intro

The microgrid concept stands for the integration of distributed generation (DG) of electricity in an existing mid/low voltage power grid infrastructure.
The size fo the microgrid is measured by its power generation capacity, and it can also have other nomenclatures due to this specification, such as mini-grid or nanogrid.
Independently of its size, it may operate disconnected from the main grid as well.
Either to feed the local point of generation or nearby points of power consumption.

However, the upcoming growth of DGs and the efficiency improvements on distributed energy resources (DER) have giving room to a new approach of the energy market for mid/low voltage power consumers.
An opportunity to choose the energy source provider beyond the distribution utility, in other words, flexibility to choose upon energy type and different electricity prices.
Unfortunately, proper coordination between this market and the current electric grid operation is a huge challenge.

The transactive energy (TE) is the concept looking to solve this issue.
The objective is to have a system to better integrate DERs, to provide transparent energy prices, and to allow consumers of all sizes to trade energy without compromise the quality and reliability of the main electricity grid.

A *one-size-fits-all* solution is impracticable but blockchain comes in on good rescue.
Although electric specifications of the distribution grid are almost the same worldwide, the electric sector directives might follow local energy capacity and potential, economic ambitions and mid-/long-term planning of the govern.
This way several blockchain solutions are being developed under the aforementioned requirements.

The current **Microgrid Transactive Energy Smart Contract** is the result of a master degree on the look for a solution to implement the TE in the Brazilian micro/mini-grid context through a blockchain application.
The thesis is also available online at the [Brazilian Thesis and Dissertation Catalog](http://catalogodeteses.capes.gov.br/catalogo-teses/#!/) [`Coming soon!`].
The smart contract developed is only a small step towards a full micro/mini-grid distributed application (Dapp) but enough to assist the power exchange between consumers and prosumers (*pro*ducers + con*sumers*) through a transparent and secure management platform.


### Smart Contract Dependency

The current project was designed for interacting with the [NEO Blockchain](https://github.com/neo-project/neo) and can be tested online with [NeoCompiler Eco](https://github.com/NeoResearch/neocompiler-eco) at the [EcoLab](https://neocompiler.io/#!/ecolab/compilers).
The `C#` algorithm implementation is in the [folder neo-dapp](/neo-dapp) under the file [`microgrid-dapp.cs`](/neo-dapp/microgrid-dapp.cs).

As additional language implementations are expected, in the folder [pseudo-code](/pseudo-code) can be found generic flowcharts and information of the proposed system, (initially) echoed from the thesis.

