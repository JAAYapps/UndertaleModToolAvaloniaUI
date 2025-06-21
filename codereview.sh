#!/bin/bash

# A simple script to collect all source code from a project into a single
# text file for code review.

# --- Configuration ---

# The root directory of the project. "." means the current directory where the script is run.
PROJECT_ROOT="."

# The name of the final output file.
OUTPUT_FILE="codebase_for_review.txt"


# --- Script Logic ---

# Announce the start of the process.
echo "Starting code collection..."
echo "Output will be saved to: $OUTPUT_FILE"

# Clear the output file if it already exists to start fresh.
> "$OUTPUT_FILE"

# This 'find' command is the heart of the script.
# It looks for files (-type f) with specific names (-name "*.cs", etc.).
# You can add or remove file extensions as needed.
#
# It specifically excludes common build and environment folders to keep the output clean.
find "$PROJECT_ROOT" -type f \( \
    -name "*.cs" -o \
    -name "*.axaml" -o \
    -name "*.csproj" -o \
    -name "*.json" -o \
    -name "*.py" -o \
    -name "*.txt" \
\) -not -path "*/bin/*" -not -path "*/obj/*" -not -path "*/.venv/*" -print0 | while IFS= read -r -d $'\0' file; do

    # For each file found, print a clear header into the output file.
    echo "====================================================================" >> "$OUTPUT_FILE"
    echo "FILE: $file" >> "$OUTPUT_FILE"
    echo "====================================================================" >> "$OUTPUT_FILE"
    echo "" >> "$OUTPUT_FILE"

    # Append the entire content of the file to our output file.
    # The `|| true` part ensures the script doesn't stop if a file has weird permissions.
    cat "$file" >> "$OUTPUT_FILE" || true

    # Add a couple of newlines for nice spacing between files.
    echo "" >> "$OUTPUT_FILE"
    echo "" >> "$OUTPUT_FILE"
done

# Announce completion.
echo "----------------------------------------------------"
echo "Code collection complete!"
echo "You can now open '$OUTPUT_FILE', copy its contents, and paste it for review."
