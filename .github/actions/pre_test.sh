#!/bin/bash

set -ex

sudo apt-get update
sudo apt-get install -y --no-install-recommends \
	libkrb5-dev \
	krb5-user \
	python3-dev \
	gcc
