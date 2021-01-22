#!/bin/sh
# copy across mounted configuration files and start c-icap service

MOUNTEDFILE=/usr/local/c-icap/conf/c-icap.conf
if [ -f "$MOUNTEDFILE" ]; then
	echo "INFO: Copying across mounted configuration"
	cp -t /usr/local/c-icap/etc /usr/local/c-icap/conf/c-icap.conf /usr/local/c-icap/rebuild/gw_rebuild.conf
else
	echo "WARNING: No mounted configuration found, running with defaults"
fi

sleep 1
echo "INFO: Starting up C-ICAP service"
/usr/local/c-icap/bin/c-icap -N -D