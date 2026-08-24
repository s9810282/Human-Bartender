import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const root = "/Users/lee/Desktop/클로드";
const inputPath = `${root}/Human-Bartender/Document/데이터/LUNA_Narrative.xlsx`;
const outputDir = `${root}/Human-Bartender/Document/outputs/2026-08-24-street-qa-examples`;
const previewDir = `${root}/Human-Bartender/Document/.codex-tmp/street-preview-after`;

const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(inputPath));

const colName = (n) => {
  let s = "";
  while (n > 0) {
    n -= 1;
    s = String.fromCharCode(65 + (n % 26)) + s;
    n = Math.floor(n / 26);
  }
  return s;
};

function readRows(sheet) {
  return sheet.getUsedRange(true).values ?? [];
}

function writeRow(sheet, rowNumber, row) {
  const lastCol = colName(row.length);
  const target = sheet.getRange(`A${rowNumber}:${lastCol}${rowNumber}`);
  if (rowNumber > 2) {
    const template = sheet.getRange(`A2:${lastCol}2`);
    target.copyFrom(template, "all");
  }
  target.values = [row];
}

function upsertRows(sheetName, rows, keyOf) {
  const sheet = workbook.worksheets.getItem(sheetName);
  let values = readRows(sheet);
  const index = new Map();
  for (let i = 1; i < values.length; i += 1) index.set(keyOf(values[i]), i + 1);
  let nextRow = values.length + 1;
  for (const row of rows) {
    const key = keyOf(row);
    const existing = index.get(key);
    if (existing) {
      writeRow(sheet, existing, row);
    } else {
      writeRow(sheet, nextRow, row);
      index.set(key, nextRow);
      nextRow += 1;
    }
  }
}

function patchById(sheetName, id, changes) {
  const sheet = workbook.worksheets.getItem(sheetName);
  const values = readRows(sheet);
  const headers = values[0];
  const rowIndex = values.findIndex((row, i) => i > 0 && row[0] === id);
  if (rowIndex < 0) throw new Error(`${sheetName}: id '${id}' not found`);
  const row = [...values[rowIndex]];
  for (const [header, value] of Object.entries(changes)) {
    const col = headers.indexOf(header);
    if (col < 0) throw new Error(`${sheetName}: column '${header}' not found`);
    row[col] = value;
  }
  writeRow(sheet, rowIndex + 1, row);
}

patchById("Characters", "radio", {
  name_ko: "아나운서",
  name_en: "Announcer",
  note: "TV·홀로그램·라디오 방송 전용 화자. 오브젝트 자동 대사에만 사용",
});

upsertRows("Characters", [
  ["street_citizen_a", "행인 A", "Passerby A", "#b9c4d0", "npc_street", false, null, "default", null, null, "[QA/Day 99] E로 시작하는 NPC 2인 대화의 화자 A"],
  ["street_citizen_b", "행인 B", "Passerby B", "#c7b8a8", "npc_street", false, null, "default", null, null, "[QA/Day 99] E로 시작하는 NPC 2인 대화의 화자 B"],
], (r) => r[0]);

upsertRows("Expressions", [
  ["street_citizen_a", "default", "sprite", "qa_street_citizen_a_default", "[QA 플레이스홀더] 기본 전신 1장"],
  ["street_citizen_b", "default", "sprite", "qa_street_citizen_b_default", "[QA 플레이스홀더] 기본 전신 1장"],
], (r) => `${r[0]}::${r[1]}`);

upsertRows("FieldAnims", [
  ["street_citizen_a", "idle", "QA/Street/citizen_a_idle", "플레이스홀더", "Day 99 NPC 대화 구현 검증용"],
  ["street_citizen_b", "idle", "QA/Street/citizen_b_idle", "플레이스홀더", "Day 99 NPC 대화 구현 검증용"],
], (r) => `${r[0]}::${r[1]}`);

upsertRows("Spots", [
  ["qa_pair_left", "qa", "QA 거리 좌측 NPC 자리", "Day 99: 행인 A·B 대화 배치"],
  ["qa_pair_right", "qa", "QA 거리 우측 NPC 자리", "Day 99: 행인 A·B 대화 배치"],
  ["qa_text_object", "qa", "QA 일반 오브젝트", "Day 99: E 조사 말풍선"],
  ["qa_sequence_object", "qa", "QA 순차 조사 오브젝트", "Day 99: sequential 선택"],
  ["qa_probe_object", "qa", "QA 조건 검증 오브젝트", "Day 99: 스텝 when·effects"],
  ["qa_monologue", "qa", "QA 1인 NPC 독백 자리", "Day 99: E로 시작하는 단일 NPC 독백"],
  ["qa_choice_npc", "qa", "QA 선택지 NPC 자리", "Day 99: continue·goto·조건 잠금 선택지"],
], (r) => r[0]);

upsertRows("InteractPoints", [
  ["p_elevator_radio", "elevator", "object", null, "commute_out", "proximity", "day == 0", "d1_elevator", "repeat", "라디오 범위 진입 시 아나운서 대사 자동 출력·자동 진행"],
  ["p_qa_pair_a", "qa_pair_left", "npc", "street_citizen_a", "both", "interact", "day == 99", "qa_np_conversation", "repeat", "A·B 둘 중 가까운 NPC에서 E → 동일 대화 시작"],
  ["p_qa_pair_b", "qa_pair_right", "npc", "street_citizen_b", "both", "interact", "day == 99", "qa_np_conversation", "repeat", "A·B 둘 중 가까운 NPC에서 E → 동일 대화 시작"],
  ["p_qa_object_text", "qa_text_object", "object", null, "both", "interact", "day == 99", "qa_object_text", "repeat", "사물 위 말풍선·수동 다음 입력"],
  ["p_qa_sequence", "qa_sequence_object", "object", null, "both", "interact", "day == 99", "group:qa_street_sequence", "sequential", "조사할 때마다 1→2→3단계, 마지막에서 멈춤"],
  ["p_qa_step_probe", "qa_probe_object", "object", null, "both", "interact", "day == 99", "qa_step_probe", "repeat", "스텝 when 건너뛰기·effects 적용 검증"],
  ["p_qa_monologue", "qa_monologue", "npc", "street_citizen_a", "both", "interact", "day == 99", "qa_npc_monologue", "repeat", "NPC 1인 독백도 플레이어가 E를 눌러야 시작"],
  ["p_qa_choice", "qa_choice_npc", "npc", "street_citizen_b", "both", "interact", "day == 99", "qa_choice", "repeat", "선택지 continue·goto·effects·조건 잠금·lock_reason 검증"],
], (r) => r[0]);

patchById("Scenes", "d1_elevator", {
  trigger: "interact",
  title: "퇴근길 엘리베이터 — 범위 진입 시 자동 출력되는 아나운서 뉴스",
});

upsertRows("Scenes", [
  ["qa_np_conversation", 99, "street", 1, "interact", null, "[QA] E로 시작하는 NPC A·B 대화", false, null],
  ["qa_object_text", 99, "street", 2, "interact", null, "[QA] 일반 사물 말풍선", false, null],
  ["qa_step_probe", 99, "street", 3, "interact", null, "[QA] 스텝 when·effects", false, null],
  ["qa_npc_monologue", 99, "street", 4, "interact", null, "[QA] E로 시작하는 1인 NPC 독백", false, null],
  ["qa_choice", 99, "street", 5, "interact", null, "[QA] 선택지 continue·goto·조건 잠금", false, null],
  ["qa_choice_result", 99, "street", 6, "manual", null, "[QA] 선택지 goto 도착 씬", false, null],
  ["qa_sequence_1", 99, "street", 10, "interact", null, "[QA] 순차 조사 1단계", false, "qa_street_sequence"],
  ["qa_sequence_2", 99, "street", 11, "interact", null, "[QA] 순차 조사 2단계", false, "qa_street_sequence"],
  ["qa_sequence_3", 99, "street", 12, "interact", null, "[QA] 순차 조사 3단계·마지막 고정", false, "qa_street_sequence"],
], (r) => r[0]);

upsertRows("Steps", [
  ["qa_np_conversation", 1, "say", "street_citizen_a", "idle", "너 그거 들었어? 애니멀 갱단 보스 그 새끼가 갑자기 프로이트 갱 놈들을 싹 쓸어버렸대.", "Did you hear? That bastard running the Animal gang suddenly wiped out a whole crew of Freud gangsters.", null, null, null, "E 상호작용 후 시작. 이동·다른 상호작용 잠금", "dlg_qa_np_conversation_001"],
  ["qa_np_conversation", 2, "say", "street_citizen_b", "idle", "뭐? 좀 잠잠하다 싶더니 또 개지랄이군.", "What? Things finally seemed quiet, and now they're raising hell again.", null, null, null, "다음 대사 입력으로만 진행", "dlg_qa_np_conversation_002"],
  ["qa_np_conversation", 3, "say", "street_citizen_a", "idle", "그러게 말이야. 하여간 짐승 새끼들 두목답다니까.", "Exactly. Figures their boss would act like the animal he is.", null, null, null, "마지막 줄 완료 후에만 이동·상호작용 잠금 해제", "dlg_qa_np_conversation_003"],
  ["qa_object_text", 1, "say", null, null, "벽에 '오늘은 새벽 3시에 전력이 끊깁니다.'라는 공지가 붙어 있다.", "A notice on the wall reads, 'Power will be cut at 3:00 a.m. today.'", null, null, null, "사물 위 공용 말풍선", "dlg_qa_object_text_001"],
  ["qa_step_probe", 1, "effect", null, null, null, null, null, "flag.qa_street_probe = true", null, "먼저 플래그 생성", null],
  ["qa_step_probe", 2, "say", null, null, "플래그가 참이므로 이 줄은 보여야 한다.", "This line must appear because the flag is true.", "flag.qa_street_probe", null, null, "보여야 정상", "dlg_qa_step_probe_001"],
  ["qa_step_probe", 3, "say", null, null, "이 줄이 보이면 스텝 when 건너뛰기가 고장 난 것이다.", "If this line appears, step-level when skipping is broken.", "!flag.qa_street_probe", null, null, "보이면 안 됨", "dlg_qa_step_probe_002"],
  ["qa_step_probe", 4, "effect", null, null, null, null, null, "flag.qa_street_probe = false", null, "반복 테스트용 리셋", null],
  ["qa_npc_monologue", 1, "say", "street_citizen_a", "idle", "어제부터 골목 자판기가 또 먹통이네.", "That alley vending machine has been broken again since yesterday.", null, null, null, "E로 시작하는 1인 NPC 독백", "dlg_qa_npc_monologue_001"],
  ["qa_npc_monologue", 2, "say", "street_citizen_a", "idle", "이 동네엔 멀쩡한 게 하나도 없어.", "Nothing in this neighborhood works the way it should.", null, null, null, "마지막 줄 후 입력 잠금 해제", "dlg_qa_npc_monologue_002"],
  ["qa_choice", 1, "say", "street_citizen_b", "idle", "선택지 실행 방식을 하나 골라 봐.", "Choose one of the choice-flow tests.", null, null, null, "선택지 QA 안내", "dlg_qa_choice_001"],
  ["qa_choice", 2, "choice", null, "ch_qa_street_choice", null, null, null, null, null, "continue·goto·조건 잠금 선택지 세트", null],
  ["qa_choice", 3, "say", "street_citizen_b", "idle", "goto가 비어 있으니 원래 씬의 다음 줄로 이어졌어.", "Because goto was empty, execution continued to the next line of the current scene.", null, null, null, "goto null 결과", "dlg_qa_choice_002"],
  ["qa_choice_result", 1, "say", "street_citizen_b", "idle", "조건이 걸린 선택지의 effects를 적용한 뒤 goto 씬으로 이동했어.", "The conditional choice applied its effects before moving to the goto scene.", "flag.qa_choice_route", null, null, "조건 선택지 goto 결과", "dlg_qa_choice_result_001"],
  ["qa_choice_result", 2, "say", "street_citizen_b", "idle", "조건 없는 선택지에서 바로 goto 씬으로 이동했어.", "The unconditional choice moved directly to the goto scene.", "!flag.qa_choice_route", null, null, "무조건 선택지 goto 결과", "dlg_qa_choice_result_002"],
  ["qa_choice_result", 3, "effect", null, null, null, null, null, "flag.qa_choice_unlocked = false; flag.qa_choice_route = false", null, "반복 검증을 위해 QA 플래그 초기화", null],
  ["qa_sequence_1", 1, "say", null, null, "첫 번째 조사: 기기 표시창이 깜빡인다.", "First inspection: the device display flickers.", null, null, null, "sequential 1단계", "dlg_qa_sequence_1_001"],
  ["qa_sequence_2", 1, "say", null, null, "두 번째 조사: 표시창에 암호화된 숫자가 떠오른다.", "Second inspection: encrypted numbers appear on the display.", null, null, null, "sequential 2단계", "dlg_qa_sequence_2_001"],
  ["qa_sequence_3", 1, "say", null, null, "세 번째 조사: '접근 권한 없음.' 더 이상 변하지 않는다.", "Third inspection: 'Access denied.' It no longer changes.", null, null, null, "sequential 마지막 장면에서 고정", "dlg_qa_sequence_3_001"],
], (r) => `${r[0]}::${r[1]}`);

upsertRows("Choices", [
  ["ch_qa_street_choice", 1, "현재 씬을 계속 본다.", "Continue the current scene.", null, "flag.qa_choice_unlocked = true", null, "goto null·effects 검증", null, null],
  ["ch_qa_street_choice", 2, "숨겨진 통로를 묻는다.", "Ask about the hidden passage.", "flag.qa_choice_unlocked", "flag.qa_choice_route = true", "qa_choice_result", "조건 활성·goto 검증", "먼저 '현재 씬을 계속 본다'를 선택해야 합니다.", "Choose 'Continue the current scene' first."],
  ["ch_qa_street_choice", 3, "바로 결과 씬으로 간다.", "Go straight to the result scene.", null, null, "qa_choice_result", "무조건 goto 검증", null, null],
], (r) => `${r[0]}::${r[1]}`);

await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });
const out1 = await SpreadsheetFile.exportXlsx(workbook);
await out1.save(`${outputDir}/LUNA_Narrative.xlsx`);
const out2 = await SpreadsheetFile.exportXlsx(workbook);
await out2.save(inputPath);

for (const [sheetName, range] of [
  ["Characters", "A1:L22"],
  ["InteractPoints", "A1:J12"],
  ["Scenes", "A45:I60"],
  ["Steps", "A838:L864"],
  ["Choices", "A38:J48"],
]) {
  const image = await workbook.render({ sheetName, range, scale: 1.25, format: "png" });
  await fs.writeFile(`${previewDir}/${sheetName}.png`, new Uint8Array(await image.arrayBuffer()));
}

const summary = {};
for (const name of ["Characters", "Expressions", "FieldAnims", "Spots", "InteractPoints", "Scenes", "Steps", "Choices"]) {
  const sheet = workbook.worksheets.getItem(name);
  const used = sheet.getUsedRange(true);
  summary[name] = used.address;
}
console.log(JSON.stringify(summary, null, 2));
