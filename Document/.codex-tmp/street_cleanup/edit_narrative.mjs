import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';
import fs from 'node:fs/promises';

const root = '/Users/lee/Desktop/클로드/Human-Bartender/Document';
const input = `${root}/데이터/LUNA_Narrative.xlsx`;
const outputDir = `${root}/outputs/2026-08-24-street-cleanup`;
const previewDir = `${root}/.codex-tmp/street_cleanup/previews_after`;
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(input));

function rewriteSheet(sheetName, transform) {
  const sheet = workbook.worksheets.getItem(sheetName);
  const used = sheet.getUsedRange(true);
  const values = used.values;
  const colCount = Math.max(...values.map((row) => row.length));
  const normalized = values.map((row) => Array.from({ length: colCount }, (_, i) => row[i] ?? null));
  const changed = transform(normalized);
  used.clear({ applyTo: 'contents' });
  sheet.getRangeByIndexes(0, 0, changed.length, colCount).values = changed;
}

function filterByFirstColumn(rows, removedIds) {
  return rows.filter((row, index) => index === 0 || !removedIds.has(String(row[0] ?? '')));
}

rewriteSheet('Characters', (rows) => filterByFirstColumn(rows, new Set(['vendor', 'sign'])));
rewriteSheet('Expressions', (rows) => filterByFirstColumn(rows, new Set(['vendor', 'sign'])));
rewriteSheet('FieldAnims', (rows) => {
  const kept = filterByFirstColumn(rows, new Set(['vendor']));
  for (const row of kept) {
    if (row[0] === 'bubi' && row[1] === 'idle') row[4] = '거리 고양이 전신 표시·배회';
  }
  return kept;
});
rewriteSheet('Spots', (rows) => filterByFirstColumn(rows, new Set(['street_stall'])));
rewriteSheet('InteractPoints', (rows) => {
  const kept = filterByFirstColumn(rows, new Set(['p_vendor_intro', 'p_vendor_shop']));
  for (const row of kept) {
    if (row[0] === 'p_alley_cat') {
      row[2] = 'npc';
      row[3] = 'bubi';
      row[9] = '고양이 전신 표시 — 플레이어가 E 상호작용할 때만 대사 시작';
    }
    if (row[0] === 'p_shiba') {
      row[9] = '시바견 NPC — 플레이어가 E 상호작용할 때만 대사 시작';
    }
  }
  return kept;
});

const removedStreetScenes = new Set(['d2_vendor_intro', 'd2_meet', 'd3_samho_death', 'd3_samho_rescue']);
rewriteSheet('Scenes', (rows) => {
  const kept = filterByFirstColumn(rows, removedStreetScenes);
  for (const row of kept) {
    if (row[0] === 'd3_alley_cat') row[6] = '골목의 고양이';
  }
  return kept;
});
rewriteSheet('Steps', (rows) => {
  const kept = filterByFirstColumn(rows, removedStreetScenes);
  for (const row of kept) {
    if (row[0] === 'd3_alley_cat' && row[2] === 'say') {
      row[3] = 'bubi';
      row[10] = '고양이 전신 캐릭터 위 말풍선 — 플레이어 상호작용 후 냐옹만 출력';
    }
  }
  return kept;
});
rewriteSheet('Cutscenes', (rows) => {
  for (const row of rows) {
    if (row[0] === 'tl_samho_meet') row[3] = '거리 시스템 제외 — 삼호·시바 첫 만남 별도 연출 컷신';
    if (row[0] === 'tl_samho_arrive') row[3] = '거리 시스템 제외 — 삼호 생존 결과·도움 요청 별도 연출 컷신';
    if (row[0] === 'tl_samho_dead') row[3] = '거리 시스템 제외 — 삼호 사망 결과 별도 연출 컷신';
  }
  return rows;
});

await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });
const exported = await SpreadsheetFile.exportXlsx(workbook);
await exported.save(`${outputDir}/LUNA_Narrative.xlsx`);

for (const sheetName of ['Characters', 'Expressions', 'FieldAnims', 'Spots', 'InteractPoints', 'Scenes', 'Cutscenes']) {
  const preview = await workbook.render({ sheetName, autoCrop: 'all', scale: 1, format: 'png' });
  await fs.writeFile(`${previewDir}/${sheetName}.png`, new Uint8Array(await preview.arrayBuffer()));
}
for (const [name, start, end] of [['Steps_A', 1, 80], ['Steps_B', 80, 120]]) {
  const preview = await workbook.render({ sheetName: 'Steps', range: `A${start}:L${end}`, scale: 1, format: 'png' });
  await fs.writeFile(`${previewDir}/${name}.png`, new Uint8Array(await preview.arrayBuffer()));
}

const checks = {};
for (const sheetName of ['Characters', 'Expressions', 'FieldAnims', 'Spots', 'InteractPoints', 'Scenes', 'Steps', 'Cutscenes']) {
  const values = workbook.worksheets.getItem(sheetName).getUsedRange(true).values;
  checks[sheetName] = { rows: values.length, cols: Math.max(...values.map((row) => row.length)) };
}
console.log(JSON.stringify(checks, null, 2));
