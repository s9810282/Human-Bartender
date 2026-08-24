import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const inputPath = "/Users/lee/Desktop/클로드/Human-Bartender/Document/데이터/LUNA_Narrative.xlsx";
const previewDir = "/Users/lee/Desktop/클로드/Human-Bartender/Document/.codex-tmp/street-preview-before";
await fs.mkdir(previewDir, { recursive: true });

const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(inputPath));
for (const sheetName of ["Characters", "FieldAnims", "Spots", "InteractPoints", "Scenes", "Steps"]) {
  const sheet = workbook.worksheets.getItem(sheetName);
  const used = sheet.getUsedRange(true);
  const info = await workbook.inspect({
    kind: "region",
    sheetId: sheetName,
    range: used.address,
    maxChars: 5000,
    tableMaxRows: 8,
    tableMaxCols: 14,
    tableMaxCellChars: 100,
  });
  console.log(`\n### ${sheetName}\n${info.ndjson}`);
  const preview = await workbook.render({ sheetName, autoCrop: "all", scale: 1, format: "png" });
  await fs.writeFile(`${previewDir}/${sheetName}.png`, new Uint8Array(await preview.arrayBuffer()));
}
