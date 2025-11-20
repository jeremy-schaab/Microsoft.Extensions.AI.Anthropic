#!/bin/bash
# Azure Foundry Basic Example - Bash Runner
# This script helps you quickly run the example with environment variables

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

echo -e "${CYAN}Azure Foundry Basic Example - Quick Start${NC}"
echo -e "${CYAN}=========================================${NC}"
echo ""

# Parse command-line arguments
RESOURCE_NAME=""
API_KEY=""
USE_AZURE_IDENTITY=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -r|--resource)
            RESOURCE_NAME="$2"
            shift 2
            ;;
        -k|--api-key)
            API_KEY="$2"
            shift 2
            ;;
        -i|--azure-identity)
            USE_AZURE_IDENTITY=true
            shift
            ;;
        -h|--help)
            echo "Usage: ./run.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  -r, --resource <name>     Set Azure resource name"
            echo "  -k, --api-key <key>       Set API key"
            echo "  -i, --azure-identity      Use Azure Identity (DefaultAzureCredential)"
            echo "  -h, --help                Show this help message"
            echo ""
            echo "Examples:"
            echo "  ./run.sh -r my-resource -k sk-ant-foundry-xxxxx"
            echo "  ./run.sh -r my-resource --azure-identity"
            echo ""
            echo "Or create a .env file with your configuration (see .env.example)"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            echo "Use --help to see available options"
            exit 1
            ;;
    esac
done

# Load .env file if it exists
if [ -f .env ]; then
    echo -e "${GREEN}Loading environment variables from .env file...${NC}"
    while IFS='=' read -r key value; do
        # Skip comments and empty lines
        if [[ ! $key =~ ^[[:space:]]*# ]] && [[ -n $key ]]; then
            # Remove leading/trailing whitespace
            key=$(echo "$key" | xargs)
            value=$(echo "$value" | xargs)
            export "$key=$value"
            echo -e "${GRAY}  $key = $value${NC}"
        fi
    done < .env
    echo ""
fi

# Override with command-line parameters if provided
if [ -n "$RESOURCE_NAME" ]; then
    echo -e "${YELLOW}Setting ANTHROPIC_FOUNDRY_RESOURCE from parameter...${NC}"
    export ANTHROPIC_FOUNDRY_RESOURCE="$RESOURCE_NAME"
fi

if [ -n "$API_KEY" ]; then
    echo -e "${YELLOW}Setting ANTHROPIC_FOUNDRY_API_KEY from parameter...${NC}"
    export ANTHROPIC_FOUNDRY_API_KEY="$API_KEY"
fi

if [ "$USE_AZURE_IDENTITY" = true ]; then
    echo -e "${YELLOW}Using Azure Identity (removing API key)...${NC}"
    unset ANTHROPIC_FOUNDRY_API_KEY
fi

# Validate configuration
if [ -z "$ANTHROPIC_FOUNDRY_RESOURCE" ]; then
    echo -e "${RED}ERROR: ANTHROPIC_FOUNDRY_RESOURCE is not set!${NC}"
    echo ""
    echo -e "${YELLOW}Usage:${NC}"
    echo "  ./run.sh -r <your-resource-name> -k <your-api-key>"
    echo "  ./run.sh -r <your-resource-name> --azure-identity"
    echo ""
    echo "Or create a .env file with your configuration (see .env.example)"
    exit 1
fi

AUTH_METHOD="Azure Identity (DefaultAzureCredential)"
if [ -n "$ANTHROPIC_FOUNDRY_API_KEY" ]; then
    AUTH_METHOD="API Key"
fi

echo -e "${GREEN}Configuration:${NC}"
echo "  Resource: $ANTHROPIC_FOUNDRY_RESOURCE"
echo "  Authentication: $AUTH_METHOD"
echo ""

# Run the example
echo -e "${CYAN}Running example...${NC}"
echo ""

if dotnet run; then
    echo ""
    echo -e "${GREEN}Example completed successfully!${NC}"
else
    EXIT_CODE=$?
    echo ""
    echo -e "${RED}Example failed with exit code: $EXIT_CODE${NC}"
    exit $EXIT_CODE
fi
