---
layout: default
title: About it
nav_order: 4
---

# About the project

The **Microgrid Transactive Energy Smart Contract (MTEsm)** is the outcome of a master degree on the look for a solution to implement the Transactive Energy (TE) in the Brazilian micro/mini-grid context through a blockchain application.

The thesis is available online at the [Brazilian Thesis and Dissertation Catalog](http://catalogodeteses.capes.gov.br/catalogo-teses/#!/) [`Coming soon!`], which presents
the worldwide efforts to implement the blockchain technology into the energy sector, a comparison between several blockchain platforms and the requirements considered for the Brazilian scenario.

The smart contract developed is only a small step towards a full micro/mini-grid distributed application (Dapp) but
enough to assist in the power exchange between consumers and prosumers (*pro*ducers + con*sumers*) through a transparent and secure management platform.

## Where it comes from

The microgrid concept stands for the integration of distributed generation (DG) of electricity in an existing mid/low voltage power grid infrastructure.
The size of the microgrid is measured by its power generation capacity, and it can also have other nomenclatures due to this specification, such as mini-grid or nanogrid.
Independently of its size, it may operate disconnected from the main grid as well, either to feed the local point of generation or nearby points of power consumption.

However, the upcoming growth of DGs and the improvements on the distributed energy resources (DER) efficiency have been giving room to a new approach in the energy market for mid/low voltage power consumers.
An opportunity to choose the energy source provider beyond the distribution utility, in other words, flexibility to choose upon energy type and different electricity prices.
Unfortunately, proper coordination between this market and the current electric grid operation is a huge challenge.

The transactive energy (TE) is the concept looking to solve this issue.
The objective is to have a system to better integrate DERs, to provide transparent energy prices,
and to allow consumers of all sizes to trade energy without compromise the quality and reliability of the main electricity grid.

A *one-size-fits-all* solution is impracticable but blockchain comes in on good rescue.
Although electric specifications of the distribution grid are almost the same worldwide,
the electric sector directives might follow local energy capacity and potential, economic ambitions and mid-/long-term planning of the govern.
This way several blockchain solutions are being developed under the aforementioned requirements.


## The blockchain used

The current project was designed for interacting with the [NEO Blockchain](https://neo.org/) and can be tested online with [NeoCompiler Eco](https://github.com/NeoResearch/neocompiler-eco) at the [EcoLab](https://neocompiler.io/#!/ecolab/compilers).
The `C#` algorithm developed is in the [GitHub](https://github.com/yurigabrich/microgrid-dapp) repository with the most updated version in the [releases history](https://github.com/yurigabrich/microgrid-dapp/releases).


## License

This project is open source and the code is shared under the [MIT License attribution](https://github.com/yurigabrich/microgrid-dapp/blob/master/LICENSE).

### Contributing

You're welcome to contribute to the project development!
I'm still a newbie on code collaboration but the following [Code of Conduct](https://github.com/yurigabrich/microgrid-dapp/blob/master/CONTRIBUTING.md) adapted from the [Contributor Covenant](https://www.contributor-covenant.org/) looks fair for this project.
Take a look at it and let's work together. 😎

---

#### Thank you to the contributors of the MTEsm!

<ul class="list-style-none">
{% for contributor in site.github.contributors %}
  {% if contributor.login != "dependabot[bot]" %}
    <li class="d-inline-block mr-1">
       <a href="{{ contributor.html_url }}"><img src="{{ contributor.avatar_url }}" width="32" height="32" alt="{{ contributor.login }}"/></a>
    </li>
  {% endif %}
{% endfor %}
</ul>
