#!/bin/sh
# copy across mounted configuration files and start c-icap service
echo "INFO: Copying across mounted configuration"
cp -t /usr/local/c-icap/etc /usr/local/c-icap/conf/c-icap.conf /usr/local/c-icap/gw-rebuid-conf/gw_rebuild.conf

sleep 5
echo "INFO: Starting up C-ICAP service"
/usr/local/c-icap/bin/c-icap -N -D