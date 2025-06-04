#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Function to display usage
usage() {
    echo "Usage: $0 <markdown-file> <page-id> [options]"
    echo ""
    echo "Arguments:"
    echo "  <markdown-file>    Path to the markdown file to sync"
    echo "  <page-id>          Confluence page ID to update (use 'new' to create)"
    echo ""
    echo "Options:"
    echo "  --parent <id>      Parent page ID (when creating new page)"
    echo "  --title <title>    Page title (optional)"
    echo "  --space <key>      Space key (when creating new page without parent)"
    echo ""
    echo "Examples:"
    echo "  # Update existing page"
    echo "  $0 ./docs/README.md 12345678"
    echo ""
    echo "  # Create new child page"
    echo "  $0 ./docs/guide.md new --parent 12345678 --title \"User Guide\""
    echo ""
    echo "  # Create new page in space"
    echo "  $0 ./docs/guide.md new --space MYSPACE --title \"User Guide\""
    exit 1
}

# Check if we have minimum arguments
if [ $# -lt 2 ]; then
    usage
fi

# Parse arguments
MARKDOWN_FILE=$1
PAGE_ID=$2
shift 2

# Parse optional arguments
PARENT_ID=""
TITLE=""
SPACE=""

while [ $# -gt 0 ]; do
    case $1 in
        --parent)
            PARENT_ID=$2
            shift 2
            ;;
        --title)
            TITLE=$2
            shift 2
            ;;
        --space)
            SPACE=$2
            shift 2
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            usage
            ;;
    esac
done

# Check if dotnet-script is installed
if ! command -v dotnet-script &> /dev/null; then
    echo -e "${YELLOW}dotnet-script is not installed. Installing...${NC}"
    dotnet tool install -g dotnet-script
    export PATH="$PATH:$HOME/.dotnet/tools"
fi

# Check environment variables
if [ -z "$CONFLUENCE_EMAIL" ] || [ -z "$CONFLUENCE_API_TOKEN" ]; then
    echo -e "${YELLOW}Confluence credentials not found in environment variables.${NC}"
    
    if [ -z "$CONFLUENCE_EMAIL" ]; then
        read -p "Enter your Confluence email: " email
        export CONFLUENCE_EMAIL=$email
    fi
    
    if [ -z "$CONFLUENCE_API_TOKEN" ]; then
        echo -e "${CYAN}You need an API token from: https://id.atlassian.com/manage-profile/security/api-tokens${NC}"
        read -s -p "Enter your Confluence API token: " token
        echo
        export CONFLUENCE_API_TOKEN=$token
    fi
fi

# Build the command
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SCRIPT_PATH="$SCRIPT_DIR/markdown-to-confluence.csx"

# Check if the markdown file exists
if [ ! -f "$MARKDOWN_FILE" ]; then
    echo -e "${RED}Error: Markdown file not found: $MARKDOWN_FILE${NC}"
    exit 1
fi

# Build arguments array
ARGS=("$MARKDOWN_FILE" "$PAGE_ID")

if [ "$PAGE_ID" = "new" ]; then
    if [ -n "$PARENT_ID" ]; then
        ARGS+=("--create-child" "$PARENT_ID")
    elif [ -n "$SPACE" ]; then
        ARGS+=("--space" "$SPACE")
    else
        echo -e "${RED}Error: When creating a new page, you must specify either --parent or --space${NC}"
        exit 1
    fi
fi

if [ -n "$TITLE" ]; then
    ARGS+=("--title" "$TITLE")
fi

# Execute the script
echo -e "${GREEN}Syncing $MARKDOWN_FILE to Confluence...${NC}"
dotnet script "$SCRIPT_PATH" "${ARGS[@]}"

# Function to sync all docs (example)
sync_all_docs() {
    # Define your document mappings here
    declare -A doc_mappings=(
        ["./docs/architecture/concurrency-control.md"]="115802116"
        ["./docs/architecture/handler-pattern.md"]="PAGE_ID_HERE"
        ["./docs/guides/snapshot-versioning.md"]="PAGE_ID_HERE"
        # Add more mappings as needed
    )
    
    for doc in "${!doc_mappings[@]}"; do
        if [ -f "$doc" ]; then
            echo -e "\n${CYAN}Syncing $doc...${NC}"
            "$0" "$doc" "${doc_mappings[$doc]}"
        else
            echo -e "${YELLOW}Warning: $doc not found${NC}"
        fi
    done
}

# If called with --sync-all, run the batch sync
if [ "$1" = "--sync-all" ]; then
    sync_all_docs
fi