#!/usr/bin/env python3
"""Extract text from DOCX by parsing XML directly"""

import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

def extract_docx_xml(docx_path):
    """Extract text from DOCX by reading the XML structure"""
    
    docx_path = Path(docx_path)
    
    if not docx_path.exists():
        print(f"Error: File not found: {docx_path}")
        return
    
    try:
        with zipfile.ZipFile(docx_path, 'r') as zip_ref:
            # Read the main document XML
            try:
                xml_content = zip_ref.read('word/document.xml')
            except KeyError:
                print("Error: word/document.xml not found in DOCX file")
                return
            
            # Parse the XML
            root = ET.fromstring(xml_content)
            
            # Define namespace
            namespaces = {
                'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main',
                'r': 'http://schemas.openxmlformats.org/officeDocument/2006/relationships'
            }
            
            # Extract all text from paragraphs
            for para in root.findall('.//w:p', namespaces):
                para_text = []
                for text_elem in para.findall('.//w:t', namespaces):
                    if text_elem.text:
                        para_text.append(text_elem.text)
                
                if para_text:
                    print(''.join(para_text))
            
            # Extract all tables
            for table in root.findall('.//w:tbl', namespaces):
                print("\n--- TABLE ---")
                for row in table.findall('.//w:tr', namespaces):
                    cells_text = []
                    for cell in row.findall('w:tc', namespaces):
                        cell_content = []
                        for para in cell.findall('w:p', namespaces):
                            para_text = []
                            for text_elem in para.findall('.//w:t', namespaces):
                                if text_elem.text:
                                    para_text.append(text_elem.text)
                            if para_text:
                                cell_content.append(''.join(para_text))
                        cells_text.append(' '.join(cell_content))
                    print(" | ".join(cells_text))
                print("--- END TABLE ---\n")
    
    except zipfile.BadZipFile:
        print("Error: Invalid DOCX file (not a valid ZIP)")
    except ET.ParseError as e:
        print(f"Error parsing XML: {e}")

if __name__ == "__main__":
    docx_file = r"c:\Users\mnaco\Github\tontApp\docs\TontinesApp_UserStories_MVP.docx"
    extract_docx_xml(docx_file)
