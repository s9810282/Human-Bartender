import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const path = "/Users/lee/Desktop/클로드/Human-Bartender/Document/데이터/LUNA_Narrative.xlsx";
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(path));
const errors = [];
const formulaErrors = /#(?:REF!|DIV\/0!|VALUE!|NAME\?|N\/A)/;

for (const sheetName of ["Characters", "Expressions", "FieldAnims", "Spots", "InteractPoints", "Scenes", "Steps", "Choices"]) {
  const sheet = workbook.worksheets.getItem(sheetName);
  const values = sheet.getUsedRange(true).values ?? [];
  values.forEach((row, r) => row.forEach((value, c) => {
    if (typeof value === "string" && formulaErrors.test(value)) {
      errors.push(`${sheetName}!R${r + 1}C${c + 1}: ${value}`);
    }
  }));
}

const mustExist = [
  ["InteractPoints", "p_qa_pair_a"],
  ["InteractPoints", "p_qa_pair_b"],
  ["InteractPoints", "p_qa_monologue"],
  ["InteractPoints", "p_elevator_radio"],
  ["InteractPoints", "p_qa_choice"],
  ["Scenes", "qa_np_conversation"],
  ["Scenes", "qa_npc_monologue"],
  ["Scenes", "qa_choice"],
  ["Scenes", "qa_choice_result"],
  ["Scenes", "qa_sequence_3"],
  ["Choices", "ch_qa_street_choice"],
];
for (const [sheetName, id] of mustExist) {
  const values = workbook.worksheets.getItem(sheetName).getUsedRange(true).values ?? [];
  if (!values.slice(1).some((row) => row[0] === id)) errors.push(`${sheetName}: missing ${id}`);
}

if (errors.length) {
  console.error(errors.join("\n"));
  process.exit(1);
}
console.log("Workbook QA passed: required rows present; no formula-error tokens found.");
