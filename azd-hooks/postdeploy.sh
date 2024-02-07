#!/bin/bash

echo "Loading azd .env file from current environment"

# Use the `get-values` azd command to retrieve environment variables from the `.env` file
while IFS='=' read -r key value; do
    value=$(echo "$value" | sed 's/^"//' | sed 's/"$//')
    export "$key=$value"
done <<EOF
$(azd env get-values) 
EOF

curl https://cosmosdbcosmicworks.blob.core.windows.net/cosmic-works-small/product.json > ./product.json
curl https://cosmosdbcosmicworks.blob.core.windows.net/cosmic-works-small/customer.json > ./customer.json

jq -c '.[]' ./product.json | sed 's/\\"/\\'\''/g' | while read i; do
    curl -X PUT $SERVICE_CHATSERVICEWEBAPI_ENDPOINT_URL/products -H 'Content-Type: application/json' -d "$i"
done

jq -c '.[]' ./customer.json | sed 's/\\"/\\'\''/g' | while read i; do
    curl -X PUT $SERVICE_CHATSERVICEWEBAPI_ENDPOINT_URL/customers -H 'Content-Type: application/json' -d "$i"
done
