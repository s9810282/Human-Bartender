import fs from "node:fs/promises";
import path from "node:path";

const root = "/Users/lee/Desktop/클로드";
const source = path.join(root, "junseo874.github.io/guest_parts_sim.html");
const assetDir = "/Users/lee/Desktop/9. 그래픽 리소스/4. 캐릭터 (내부)/NPC/여";
const output = path.join(
  root,
  "Human-Bartender/Document/Generated/여자_일반손님_파츠_시뮬레이터.html",
);

const fileNames = [
  "body.png",
  "top_1.png", "top_2.png", "top_3.png",
  "Acc_1.png", "Acc_2.png", "Acc_3.png",
  "Eyebrow_1.png", "Eyebrow_2.png", "Eyebrow_3.png",
  "Eye_1.png", "Eye_2.png", "Eye_3.png",
  "mouth_1.png", "mouth_2.png", "mouth_3.png",
  "Hair_1.png", "Hair_2.png",
];

const parts = {};
for (const fileName of fileNames) {
  const key = path.basename(fileName, ".png");
  const bytes = await fs.readFile(path.join(assetDir, fileName));
  parts[key] = `data:image/png;base64,${bytes.toString("base64")}`;
}

const cats = `const CATS = [
  {key:"body",    name:"바디",       layer:1, items:["body"], fixed:true},
  {key:"top",     name:"상의",       layer:2, items:["top_1","top_2","top_3"]},
  {key:"acc",     name:"액세서리",   layer:3, items:[null,"Acc_1","Acc_2","Acc_3"],
   labels:{"Acc_1":"팔 액세서리","Acc_2":"목 액세서리","Acc_3":"아우터"}},
  {key:"eyebrow", name:"눈썹",       layer:4, items:["Eyebrow_1","Eyebrow_2","Eyebrow_3"]},
  {key:"eye",     name:"눈",         layer:5, items:["Eye_1","Eye_2","Eye_3"]},
  {key:"mouth",   name:"입",         layer:6, items:["mouth_1","mouth_2","mouth_3"]},
  {key:"hair",    name:"헤어",       layer:7, items:["Hair_1","Hair_2"]},
];`;

let html = await fs.readFile(source, "utf8");
html = html
  .replace("랜덤 손님 파츠 조합 시뮬레이터 (남자)", "랜덤 손님 파츠 조합 시뮬레이터 (여자)")
  .replace("랜덤 손님 파츠 조합 시뮬레이터 — 남자", "랜덤 손님 파츠 조합 시뮬레이터 — 여자")
  .replace(
    "전 파츠가 같은 332×388 캔버스에 정렬돼 있어 좌표 보정 없이 레이어만 쌓는다.",
    "전 파츠가 같은 210×321 캔버스에 정렬돼 있어 좌표 보정 없이 레이어만 쌓는다.",
  )
  .replace("바디 → 의상 → 액세서리 → 눈썹 → 눈 → 입 → 헤어", "바디 → 상의 → 액세서리 → 눈썹 → 눈 → 입 → 헤어")
  .replace("#stage{image-rendering:pixelated; width:332px; height:388px;}", "#stage{image-rendering:pixelated; width:210px; height:321px;}")
  .replace('<canvas id="stage" width="332" height="388"></canvas>', '<canvas id="stage" width="210" height="321"></canvas>')
  .replace(/const PARTS = \{[\s\S]*?\};\n\nconst CATS = \[[\s\S]*?\n\];/, `const PARTS = ${JSON.stringify(parts)};\n\n${cats}`)
  .replace('a.download = "guest_" + Date.now() + ".png";', 'a.download = "female_guest_" + Date.now() + ".png";');

if (!html.includes("여자") || !html.includes('width="210" height="321"')) {
  throw new Error("Template replacement failed");
}

await fs.mkdir(path.dirname(output), { recursive: true });
await fs.writeFile(output, html, "utf8");
console.log(output);
