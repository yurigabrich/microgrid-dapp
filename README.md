# Micro Grid Transactive Energy Management Smart Contract

The micro grid concept stands for the integration of distributed generation (DG) of electricity in an existing mid/low voltage power grid infrastructure.
The size fo the micro grid is measure by its power generation capacity, and it can also have other nomenclatures due to this specification, such as mini or nano grid.
Independently of its size, it may opperate disconnected from the main grid as well.
Either to feed the local point of generation, or nearby points of power consumer.

However, the upcoming growth of DG and the efficiency improvements on distributed energy resources (DER) have giving room to a new approach of energy market for mid/low voltage power consumers.
An opportunity to choose the energy source provider beyond the distribution utility, in other words, flexibility to choose upon energy type and different electricity prices.
Unfortunately, a proper coordination between this market and current electric grid operation is a huge challenge.

The transactive energy (TE) is the concept looking to solve this issue.
The objective is to have a system to better integrate DERs, to provide transparent energy prices, and to allow consumers of all sizes to trade energy without compromise the quality and reliability of the main electricity grid.

A *one-size-fits-all* solution is impractible but blockchain comes as a good rescue.
Although electric specifications of the distribution grid is almost the same worldwide, the electric sector directives might follow local energy capacity and potential, economic ambitions and mid-/long-term planning of the govern.
This way several blockchain solutions are being developed under the aforementioned requirements.

The current **Micro Grid Transactive Energy Management Smart Contract** is the result of a master degree on the look for a solution for the implementation of the TE in the Brazilian micro/mini grid context.
The thesis is also available on this repo [[PDF... is coming]]().
The smart contract developed is only a small step towards a full micro/mini grid distributed application (dApp) but enough to assist the power exchange between cosumers and prosumers (*pro*ducers + con*sumers*) through a transparent and secure management platform.

## Smart Contract

The current project was designed for interacting with the [NEO Blockchain](https://github.com/neo-project/neo) and can be tested online with [NeoCompiler Eco](https://github.com/NeoResearch/neocompiler-eco) at the [EcoLab](https://neocompiler.io/#!/ecolab/compilers).
The `C#` algorithm implementation is on the [folder `dapp-csh`](neo-dapp/microgrid-dapp.cs).

As additional language implementations are expected, on the folder [pseudo-code/README.md](pseudo-code/README.md) can be found generic flowcharts and pseudocodes of the proposed system, (initially) echoed from the thesis.

## Open source license

This project is shared under the [MIT License attribution](LICENSE).

`SPDX-License-Identifier: MIT License`

## Contributing guidelines

You're really welcome to contribute to the project development!
I'm still neaby on code collaboration but the following Code of Conduct adapted from the [Contributor Covenant](https://www.contributor-covenant.org/) looks fair for this project. Take a look on it below and let's work together. :sunglasses:

<section>

section {
    box-sizing: border-box;
    margin: 0 auto;
    max-width: 47rem;
    padding: 1.5rem;
}

<h1 id="contributor-covenant-code-of-conduct">Contributor Covenant Code of Conduct</h1>

<h2 id="our-pledge">Our Pledge</h2>

<p>In the interest of fostering an open and welcoming environment, we as
contributors and maintainers pledge to make participation in our project and
our community a harassment-free experience for everyone, regardless of age, body
size, disability, ethnicity, sex characteristics, gender identity and expression,
level of experience, education, socio-economic status, nationality, personal
appearance, race, religion, or sexual identity and orientation.</p>

<h2 id="our-standards">Our Standards</h2>

<p>Examples of behavior that contributes to creating a positive environment
include:</p>

<ul>
<li>Using welcoming and inclusive language</li>
<li>Being respectful of differing viewpoints and experiences</li>
<li>Gracefully accepting constructive criticism</li>
<li>Focusing on what is best for the community</li>
<li>Showing empathy towards other community members</li>
</ul>

<p>Examples of unacceptable behavior by participants include:</p>

<ul>
<li>The use of sexualized language or imagery and unwelcome sexual attention or
advances</li>
<li>Trolling, insulting/derogatory comments, and personal or political attacks</li>
<li>Public or private harassment</li>
<li>Publishing others’ private information, such as a physical or electronic
address, without explicit permission</li>
<li>Other conduct which could reasonably be considered inappropriate in a
professional setting</li>
</ul>

<h2 id="our-responsibilities">Our Responsibilities</h2>

<p>Project maintainers are responsible for clarifying the standards of acceptable
behavior and are expected to take appropriate and fair corrective action in
response to any instances of unacceptable behavior.</p>

<p>Project maintainers have the right and responsibility to remove, edit, or
reject comments, commits, code, wiki edits, issues, and other contributions
that are not aligned to this Code of Conduct, or to ban temporarily or
permanently any contributor for other behaviors that they deem inappropriate,
threatening, offensive, or harmful.</p>

<h2 id="scope">Scope</h2>

<p>This Code of Conduct applies within all project spaces, and it also applies when
an individual is representing the project or its community in public spaces.
Examples of representing a project or community include using an official
project e-mail address, posting via an official social media account, or acting
as an appointed representative at an online or offline event. Representation of
a project may be further defined and clarified by project maintainers.</p>

<h2 id="enforcement">Enforcement</h2>

<p>Instances of abusive, harassing, or otherwise unacceptable behavior may be
reported by contacting the project team at <b>@yurigabrich</b>. All
complaints will be reviewed and investigated and will result in a response that
is deemed necessary and appropriate to the circumstances. The project team is
obligated to maintain confidentiality with regard to the reporter of an incident.
Further details of specific enforcement policies may be posted separately.</p>

<p>Project maintainers who do not follow or enforce the Code of Conduct in good
faith may face temporary or permanent repercussions as determined by other
members of the project’s leadership.</p>

<h2 id="attribution">Attribution</h2>

<p>This Code of Conduct is adapted from the <a href="https://www.contributor-covenant.org">Contributor Covenant</a>, version 1.4,
available at <a href="https://www.contributor-covenant.org/version/1/4/code-of-conduct">https://www.contributor-covenant.org/version/1/4/code-of-conduct.html</a></p>

<p>For answers to common questions about this code of conduct, see
<a href="https://www.contributor-covenant.org/faq">https://www.contributor-covenant.org/faq</a></p>

</section>




