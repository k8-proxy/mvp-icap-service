FROM ubuntu as base
RUN apt-get update && apt-get upgrade -y

FROM base as source
RUN apt-get install -y curl gcc make libtool autoconf automake automake1.11 unzip && \
    cd /tmp && mkdir c-icap
   
COPY ./c-icap/ /tmp/c-icap/c-icap/
COPY ./c-icap-modules /tmp/c-icap/c-icap-modules  

FROM source as build    
RUN cd /tmp/c-icap/c-icap &&  \
    autoreconf -i && \
    ./configure --prefix=/usr/local/c-icap && make && make install
    
RUN cd /tmp/c-icap/c-icap-modules && \
    autoreconf -i && \
    ./configure --with-c-icap=/usr/local/c-icap --prefix=/usr/local/c-icap && make && make install && \
    echo >> /usr/local/c-icap/etc/c-icap.conf && echo "Include gw_rebuild.conf" >> /usr/local/c-icap/etc/c-icap.conf
    
FROM base
COPY --from=build /usr/local/c-icap /usr/local/c-icap
COPY --from=build /run/c-icap /run/c-icap

EXPOSE 1344
CMD ["/usr/local/c-icap/bin/c-icap","-N","-D"]