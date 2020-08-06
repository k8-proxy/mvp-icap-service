# Glasswall ICAP Service - Minimum Viable Prototype
ICAP Service with ICAP Resource that interfaces with GW Cloud products

## Getting started
The original baseline code has been cloned from the open source project
https://sourceforge.net/projects/c-icap/

Demonstration ICAP Resources have been removed.

## Building ICAP Service

These instructions guide the user through the steps involved in installing the Glasswall ICAP PoC on a Linux host.

Running the follow commands will ensure the necessary packages are installed.
```
sudo apt-get update
sudo apt-get upgrade -y
sudo apt-get install git
sudo apt-get install gcc
sudo apt-get install -y doxygen
sudo apt-get install make
sudo apt-get install automake
sudo apt-get install automake1.11
```

### Build the Server
From where the repo was cloned to, navigate into the `c-icap/c-icap` folder and run the scripts to setup the Makefiles.
```
aclocal
autoconf
automake --add-missing
```
Run the configure script, specifying where the server should be installed, through the `prefix` argument.
```
./configure --prefix=/usr/local/c-icap
```
After running the configuration script, build and install the server.
```
make 
sudo make install
```
The option is available to generate the documentation, if required
```
make doc
```

