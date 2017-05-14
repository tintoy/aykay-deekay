#!/bin/bash

if [ -z $TARGET_URL ]; then
	echo "TARGET_URL environment variable is not defined."

	exit 1
fi

echo "Fetching from '$TARGET_URL'..."
curl --silent --show-error -L -o /root/state/content.txt "$TARGET_URL"

echo "Done."
