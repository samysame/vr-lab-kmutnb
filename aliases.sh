#!/bin/bash

# Destination file for aliases (Git Bash, macOS, Linux)
BASHRC=~/.bashrc

cat << 'EOF' >> $BASHRC

alias gst="git status"
alias gco="git checkout"
alias gcob="git checkout -b"
alias gbr="git branch"
alias gcm="git commit -m"
alias gad="git add"
alias gp="git push"
alias gcp="git cherry-pick"

EOF

source $BASHRC
