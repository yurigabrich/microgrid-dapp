# Microgrid Transactive Energy Management Smart Contract

The microgrid concept stands for the integration of distributed generation (DG) of electricity in an existing mid/low voltage power grid infrastructure.
The size fo the microgrid is measured by its power generation capacity, and it can also have other nomenclatures due to this specification, such as mini-grid or nano grid.
Independently of its size, it may operate disconnected from the main grid as well.
Either to feed the local point of generation or nearby points of power consumption.

However, the upcoming growth of DGs and the efficiency improvements on distributed energy resources (DER) have giving room to a new approach of the energy market for mid/low voltage power consumers.
An opportunity to choose the energy source provider beyond the distribution utility, in other words, flexibility to choose upon energy type and different electricity prices.
Unfortunately, proper coordination between this market and the current electric grid operation is a huge challenge.

The transactive energy (TE) is the concept looking to solve this issue.
The objective is to have a system to better integrate DERs, to provide transparent energy prices, and to allow consumers of all sizes to trade energy without compromise the quality and reliability of the main electricity grid.

A *one-size-fits-all* solution is impracticable but blockchain comes as a good rescue.
Although electric specifications of the distribution grid are almost the same worldwide, the electric sector directives might follow local energy capacity and potential, economic ambitions and mid-/long-term planning of the govern.
This way several blockchain solutions are being developed under the aforementioned requirements.

The current **Microgrid Transactive Energy Management Smart Contract** is the result of a master degree on the look for a solution to implement the TE in the Brazilian micro/mini-grid context.
The thesis is also available on this repo [[PDF is coming...]]().
The smart contract developed is only a small step towards a full micro/mini-grid distributed application (dApp) but enough to assist the power exchange between consumers and prosumers (*pro*ducers + con*sumers*) through a transparent and secure management platform.

## Smart Contract

The current project was designed for interacting with the [NEO Blockchain](https://github.com/neo-project/neo) and can be tested online with [NeoCompiler Eco](https://github.com/NeoResearch/neocompiler-eco) at the [EcoLab](https://neocompiler.io/#!/ecolab/compilers).
The `C#` algorithm implementation is on the [folder `dapp-csh`](neo-dapp/microgrid-dapp.cs).

As additional language implementations are expected, on the folder [pseudo-code/README.md](pseudo-code/README.md) can be found generic flowcharts and pseudocodes of the proposed system, (initially) echoed from the thesis.

## Open source license

This project is shared under the [MIT License attribution](LICENSE).

`SPDX-License-Identifier: MIT License`

## Contributing guidelines

You're welcome to contribute to the project development!
I'm still a newbie on code collaboration but the following Code of Conduct adapted from the [Contributor Covenant](https://www.contributor-covenant.org/) looks fair for this project. Take a look at it below and let's work together. :sunglasses:

<table>
<tr>
<td>

### Contributor Covenant Code of Conduct

#### Our Pledge

In the interest of fostering an open and welcoming environment, we as contributors and maintainers pledge to make participation in our project and our community a harassment-free experience for everyone, regardless of age, body size, disability, ethnicity, sex characteristics, gender identity and expression, level of experience, education, socio-economic status, nationality, personal appearance, race, religion, or sexual identity and orientation.

#### Our Standards

Examples of behaviour that contributes to creating a positive environment include:

- Using welcoming and inclusive language.
- Being respectful of differing viewpoints and experiences.
- Gracefully accepting constructive criticism.
- Focusing on what is best for the community.
- Showing empathy towards other community members.

Examples of unacceptable behaviour by participants include:

- The use of sexualized language or imagery and unwelcome sexual attention or advances.
- Trolling, insulting/derogatory comments, and personal or political attacks.
- Public or private harassment.
- Publishing others' private information, such as a physical or electronic address, without explicit permission.
- Other conduct which could reasonably be considered inappropriate in a professional setting.

#### Our Responsibilities

Project maintainers are responsible for clarifying the standards of acceptable behaviour and are expected to take appropriate and fair corrective action in response to any instances of unacceptable behaviour.

Project maintainers have the right and responsibility to remove, edit, or reject comments, commits, code, wiki edits, issues, and other contributions that are not aligned to this Code of Conduct, or to ban temporarily or permanently any contributor for other behaviors that they deem inappropriate, threatening, offensive, or harmful.

#### Scope

This Code of Conduct applies within all project spaces, and it also applies when an individual is representing the project or its community in public spaces.
Examples of representing a project or community include using an official project e-mail address, posting via an official social media account, or acting as an appointed representative at an online or offline event. Representation of a project may be further defined and clarified by project maintainers.

#### Enforcement

Instances of abusive, harassing, or otherwise unacceptable behaviour may be reported by contacting anyone from the project team.
All complaints will be reviewed and investigated and will result in a response that is deemed necessary and appropriate to the circumstances.
The project team is obligated to maintain confidentiality with regard to the reporter of an incident.
Further details of specific enforcement policies may be posted separately.

Project maintainers who do not follow or enforce the Code of Conduct in good faith may face temporary or permanent repercussions as determined by other members of the project's leadership.

#### Attribution

This Code of Conduct is adapted from the [Contributor Covenant][homepage], version 1.4,
available at https://www.contributor-covenant.org/version/1/4/code-of-conduct.html

[homepage]: https://www.contributor-covenant.org

For answers to common questions about this code of conduct, see
https://www.contributor-covenant.org/faq

</td>
</tr>
</table>