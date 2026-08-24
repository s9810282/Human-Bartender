import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';
import fs from 'node:fs/promises';

const input = '/Users/lee/Desktop/클로드/Human-Bartender/Document/데이터/LUNA_Narrative.xlsx';
const outDir = '/Users/lee/Desktop/클로드/Human-Bartender/Document/.codex-tmp/street_cleanup/previews_before';
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(input));

const sheets = ['Characters', 'Expressions', 'FieldAnims', 'Spots', 'InteractPoints', 'Scenes', 'Cutscenes'];
for (const sheet of sheets) {
  const ws = workbook.worksheets.getItem(sheet);
  const used = ws.getUsedRange(true);
  const values = used.values;
  console.log(`\n===== ${sheet} =====`);
  console.log(`rows=${values.length} cols=${Math.max(...values.map((r) => r.length))}`);
  console.log(JSON.stringify(values.map((row, index) => ({excelRow: index + 1, row})).filter(({row, excelRow}) => {
    if (excelRow === 1) return true;
    const hay = row.map((v) => String(v ?? '')).join('|');
    return /(vendor|완|street_stall|d2_vendor_intro|d2_meet|d3_alley_cat|d3_samho_death|d3_samho_rescue|p_alley_cat|bubi|shiba|tl_samho)/i.test(hay);
  }), null, 2));
  const render = await workbook.render({ sheetName: sheet, autoCrop: 'all', scale: 1, format: 'png' });
  await fs.writeFile(`${outDir}/${sheet}.png`, new Uint8Array(await render.arrayBuffer()));
}

const steps = workbook.worksheets.getItem('Steps').getUsedRange(true).values;
const targetIds = new Set(['d2_vendor_intro', 'd2_meet', 'd3_samho_death', 'd3_samho_rescue', 'd3_alley_cat']);
const stepRows = steps.map((row, index) => ({excelRow: index + 1, row})).filter(({row, excelRow}) => excelRow === 1 || targetIds.has(row[0]));
console.log('\n===== Steps targets =====');
console.log(JSON.stringify(stepRows, null, 2));
for (const [name, start, end] of [['Steps_A', 1, 80], ['Steps_B', 80, 145]]) {
  const render = await workbook.render({ sheetName: 'Steps', range: `A${start}:L${end}`, scale: 1, format: 'png' });
  await fs.writeFile(`${outDir}/${name}.png`, new Uint8Array(await render.arrayBuffer()));
}
