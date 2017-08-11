Secure multi-party computation (MPC) is one of the most promising approaches to
ensure confidentiality of data used in computations in an untrusted environment. In
MPC a client outsources computations on confidential data to a network of servers.
The client does not trust a single server, but believes that multiple servers do not
collude. In this project ourmain focus was on efficient numerical computations. For
that purpose we derived new protocols and extended existing protocols for various
operations based on the JOS scheme, which is based on linear secret sharing. The
main contribution is an implementation of the JOS system with our extensions covering
several novel aspects that have not been addressed in priorMPC systems, e.g.
dependence graph analysis and code parallelization, code generation at runtime and
sub-domain privacy.
