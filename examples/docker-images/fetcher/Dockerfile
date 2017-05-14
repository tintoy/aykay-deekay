FROM ubuntu:xenial

# Dependencies
RUN apt-get update && apt-get install -y curl

# The target URL to fetch.
ENV TARGET_URL="https://www.google.com/"

# The container entry-point.
COPY entrypoint.sh /root

# The state directory where the fetched content will be saved.
RUN mkdir -p /root/state
VOLUME /root/state
WORKDIR /root/state

ENTRYPOINT ["/bin/bash", "/root/entrypoint.sh"]
