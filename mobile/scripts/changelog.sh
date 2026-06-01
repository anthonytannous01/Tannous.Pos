#!/bin/bash

# Changelog generator for Tannous POS Android App
# Generates release notes from conventional commits since last tag

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}Generating changelog for Tannous POS...${NC}"

# Get the last tag
LAST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0")
echo -e "${GREEN}Last tag: ${LAST_TAG}${NC}"

# Create ci directory if it doesn't exist
mkdir -p ci

# Get commits since last tag
COMMITS=$(git log --pretty=format:"%s" ${LAST_TAG}..HEAD 2>/dev/null || echo "")

if [ -z "$COMMITS" ]; then
    echo -e "${YELLOW}No new commits since last tag. Using fallback changelog.${NC}"
    cat > ci/release-notes.txt << EOF
Release Notes - Tannous POS

No new commits since ${LAST_TAG}

This is an automated release with no new features or fixes.
EOF
else
    echo -e "${GREEN}Found commits since ${LAST_TAG}${NC}"
    
    # Initialize changelog
    cat > ci/release-notes.txt << EOF
Release Notes - Tannous POS

Version: $(git describe --tags --abbrev=0 2>/dev/null | sed 's/^v//' || echo "1.0.0")
Release Date: $(date '+%Y-%m-%d %H:%M:%S UTC')
Commit Range: ${LAST_TAG}..$(git rev-parse --short HEAD)

EOF

    # Group commits by type
    echo -e "${BLUE}Grouping commits by type...${NC}"
    
    # Features
    FEATURES=$(echo "$COMMITS" | grep -i "^feat" || true)
    if [ ! -z "$FEATURES" ]; then
        echo "## ✨ New Features" >> ci/release-notes.txt
        echo "$FEATURES" | sed 's/^feat[^:]*:/- /' | sed 's/^feat/- /' >> ci/release-notes.txt
        echo "" >> ci/release-notes.txt
    fi
    
    # Fixes
    FIXES=$(echo "$COMMITS" | grep -i "^fix" || true)
    if [ ! -z "$FIXES" ]; then
        echo "## 🐛 Bug Fixes" >> ci/release-notes.txt
        echo "$FIXES" | sed 's/^fix[^:]*:/- /' | sed 's/^fix/- /' >> ci/release-notes.txt
        echo "" >> ci/release-notes.txt
    fi
    
    # Performance improvements
    PERF=$(echo "$COMMITS" | grep -i "^perf" || true)
    if [ ! -z "$PERF" ]; then
        echo "## ⚡ Performance Improvements" >> ci/release-notes.txt
        echo "$PERF" | sed 's/^perf[^:]*:/- /' | sed 's/^perf/- /' >> ci/release-notes.txt
        echo "" >> ci/release-notes.txt
    fi
    
    # Refactoring
    REFACTOR=$(echo "$COMMITS" | grep -i "^refactor" || true)
    if [ ! -z "$REFACTOR" ]; then
        echo "## 🔧 Refactoring" >> ci/release-notes.txt
        echo "$REFACTOR" | sed 's/^refactor[^:]*:/- /' | sed 's/^refactor/- /' >> ci/release-notes.txt
        echo "" >> ci/release-notes.txt
    fi
    
    # Documentation
    DOCS=$(echo "$COMMITS" | grep -i "^docs" || true)
    if [ ! -z "$DOCS" ]; then
        echo "## 📚 Documentation" >> ci/release-notes.txt
        echo "$DOCS" | sed 's/^docs[^:]*:/- /' | sed 's/^docs/- /' >> ci/release-notes.txt
        echo "" >> ci/release-notes.txt
    fi
    
    # Chores and other commits
    OTHERS=$(echo "$COMMITS" | grep -iv "^feat\|^fix\|^perf\|^refactor\|^docs\|^chore" | grep -v "^$" || true)
    if [ ! -z "$OTHERS" ]; then
        echo "## 🔄 Other Changes" >> ci/release-notes.txt
        echo "$OTHERS" | sed 's/^/- /' >> ci/release-notes.txt
        echo "" >> ci/release-notes.txt
    fi
    
    # Add footer
    cat >> ci/release-notes.txt << EOF
---
Generated automatically from Git commits.
For detailed information, see the Git history.
EOF

    echo -e "${GREEN}Changelog generated successfully!${NC}"
fi

# Display the generated changelog
echo -e "${BLUE}Generated changelog:${NC}"
echo "----------------------------------------"
cat ci/release-notes.txt
echo "----------------------------------------"

echo -e "${GREEN}Changelog saved to: ci/release-notes.txt${NC}"
