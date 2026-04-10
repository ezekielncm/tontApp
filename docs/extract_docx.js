const fs = require('fs');
const AdmZip = require('adm-zip');
const xml2js = require('xml2js');

async function extractDocx(docxPath) {
    try {
        // Read the DOCX file as a ZIP
        const zip = new AdmZip(docxPath);
        
        // Get the document.xml entry
        const docEntry = zip.getEntry('word/document.xml');
        if (!docEntry) {
            console.error('Error: word/document.xml not found in DOCX');
            return;
        }
        
        const xmlContent = zip.readAsText(docEntry);
        
        // Parse XML
        const parser = new xml2js.Parser();
        const result = await parser.parseStringPromise(xmlContent);
        
        // Extract document root
        const document = result['w:document'];
        if (!document || !document['w:body']) {
            console.error('Error: Invalid document structure');
            return;
        }
        
        const body = document['w:body'][0];
        
        // Process all elements (paragraphs and tables)
        if (body['w:p']) {
            body['w:p'].forEach(para => {
                let paraText = '';
                if (para['w:r']) {
                    para['w:r'].forEach(run => {
                        if (run['w:t'] && run['w:t'][0]) {
                            paraText += run['w:t'][0];
                        }
                    });
                }
                if (paraText.trim()) {
                    console.log(paraText);
                }
            });
        }
        
        if (body['w:tbl']) {
            body['w:tbl'].forEach(table => {
                console.log('\n--- TABLE ---');
                if (table['w:tr']) {
                    table['w:tr'].forEach(row => {
                        const cells = [];
                        if (row['w:tc']) {
                            row['w:tc'].forEach(cell => {
                                let cellText = '';
                                if (cell['w:p']) {
                                    cell['w:p'].forEach(para => {
                                        if (para['w:r']) {
                                            para['w:r'].forEach(run => {
                                                if (run['w:t'] && run['w:t'][0]) {
                                                    cellText += run['w:t'][0];
                                                }
                                            });
                                        }
                                    });
                                }
                                cells.push(cellText);
                            });
                        }
                        console.log(cells.join(' | '));
                    });
                }
                console.log('--- END TABLE ---\n');
            });
        }
    } catch (error) {
        console.error('Error:', error.message);
    }
}

const docxFile = process.argv[2] || 'c:\\Users\\mnaco\\Github\\tontApp\\docs\\TontinesApp_UserStories_MVP.docx';
extractDocx(docxFile);
