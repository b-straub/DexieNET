#!/bin/bash -e

docker buildx build --no-cache --platform=linux/amd64 -t pushserver ./
docker save -o pushserver.tar pushserver:latest