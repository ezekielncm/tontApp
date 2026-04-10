#!/usr/bin/env python3
"""
Extract text from DOCX using only Python standard library.
Uses zipfile and xml.etree to parse the document.xml from the DOCX file.
"""

import zipfile
import xml.etree.ElementTree as ET
import sys
import os

# Path to the DOCX file
docx_path = r"c:\Users\mnaco\Github\tontApp\docs\TontinesApp_UserStories_MVP.docx"

if not os.path.exists(docx_path):
    print(f"ERROR: File not found at {docx_path}")
    sys.exit(1)

try:
    # Open DOCX as ZIP
    with zipfile.ZipFile(docx_path, 'r') as docx_zip:
        # Read the main document XML
        xml_content = docx_zip.read('word/document.xml')
        
except Exception as e:
    print(f"ERROR reading DOCX: {e}")
    sys.exit(1)

# Parse XML
try:
    root = ET.fromstring(xml_content)
except Exception as e:
    print(f"ERROR parsing XML: {e}")
    sys.exit(1)

# Define Word namespace
ns = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}

# Extract text from paragraphs
print("=== EXTRACTING TEXT FROM DOCUMENT ===\n")

body = root.find('.//w:body', ns)
if body is None:
    print("ERROR: Could not find document body")
    sys.exit(1)

# Process all elements in body
for element in body:
    # Check if it's a paragraph
    if element.tag == '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}p':
        # Extract text from paragraph
        para_text = []
        for run in element.findall('.//w:t', ns):
            if run.text:
                para_text.append(run.text)
        
        if para_text:
            print(''.join(para_text))
    
    # Check if it's a table
    elif element.tag == '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}tbl':
        print("\n" + "="*80)
        print("TABLE START")
        print("="*80)
        
        # Extract table rows
        for row in element.findall('w:tr', ns):
            cell_contents = []
            for cell in row.findall('w:tc', ns):
                # Extract text from cell
                cell_text = []
                for para in cell.findall('w:p', ns):
                    for text_elem in para.findall('.//w:t', ns):
                        if text_elem.text:
                            cell_text.append(text_elem.text)
                
                cell_contents.append(' '.join(cell_text))
            
            # Print row
            print(" | ".join(cell_contents))
        
        print("="*80)
        print("TABLE END")
        print("="*80 + "\n")

print("\n=== END OF DOCUMENT ===")
