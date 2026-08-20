#!/bin/bash

# Destination file for aliases (Git Bash, macOS, Linux)
BASHRC=~/.bashrc

echo "Adding aliases to file $BASHRC..."

cat << 'EOF' >> $BASHRC

alias gst="git status"
alias gco="git checkout"
alias gcob="git checkout -b"
alias gbr="git branch"
alias gcm="git commit -m"
alias gad="git add"
EOF

source $BASHRC
echo "Aliases successfully installed !"
