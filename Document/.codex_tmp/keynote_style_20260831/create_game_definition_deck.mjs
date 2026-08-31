import fs from "node:fs/promises";
import path from "node:path";
import { importRuntimeModule } from "/Users/lee/.codex/plugins/cache/openai-primary-runtime/presentations/26.826.12353/skills/presentations/container_tools/runtime_helpers.mjs";

const ROOT = "/Users/lee/Desktop/클로드/Human-Bartender/Document/.codex_tmp/keynote_style_20260831";
const STARTER = path.join(ROOT, "template-starter.pptx");
const RENDER_DIR = path.join(ROOT, "final-render");
const LAYOUT_DIR = path.join(ROOT, "final-layout");
const MONTAGE = path.join(ROOT, "final-montage.webp");
const OUTPUT = "/Users/lee/Desktop/클로드/Human-Bartender/Document/Generated/게임은_무엇인가_5장_발표자료.pptx";

async function writeBlob(filePath, blob) {
  await fs.writeFile(filePath, new Uint8Array(await blob.arrayBuffer()));
}

function makeTextIndex(snapshot) {
  const index = new Map();
  const orderBySlide = new Map();
  for (const line of snapshot.ndjson.split("\n")) {
    if (!line.trim()) continue;
    const item = JSON.parse(line);
    if ((item.kind === "textbox" || item.kind === "shape") && item.text && item.id && item.slide) {
      index.set(`${item.slide}\u0000${item.text.normalize("NFC")}`, item.id);
      if (!orderBySlide.has(item.slide)) orderBySlide.set(item.slide, []);
      orderBySlide.get(item.slide).push(item.id);
    }
  }
  index.orderBySlide = orderBySlide;
  index.usedIds = new Set();
  return index;
}

function setText(presentation, textIndex, slideNumber, originalText, value) {
  const normalized = originalText.normalize("NFC");
  let anchorId = textIndex.get(`${slideNumber}\u0000${normalized}`);
  if (!anchorId) {
    const canonical = (text) => text.normalize("NFC").replace(/[\s.,?!:·\-]/g, "");
    const prefix = canonical(normalized).slice(0, 6);
    const matches = [...textIndex.entries()].filter(([key]) => {
      const [slide, text] = key.split("\u0000");
      return Number(slide) === slideNumber && canonical(text).startsWith(prefix);
    });
    if (matches.length === 1) anchorId = matches[0][1];
  }
  if (!anchorId) {
    anchorId = (textIndex.orderBySlide.get(slideNumber) || []).find((id) => !textIndex.usedIds.has(id));
  }
  if (!anchorId) {
    const available = [...textIndex.keys()].filter((key) => key.startsWith(`${slideNumber}\u0000`));
    throw new Error(`Could not resolve text on slide ${slideNumber}: ${originalText}\nAvailable: ${available.join(" | ")}`);
  }
  textIndex.usedIds.add(anchorId);
  const shape = presentation.resolve(anchorId);
  const normalizedValue = value
    .normalize("NFC")
    .replaceAll("\uad6c\uce59", "\uaddc\uce59")
    .replaceAll("\uc9c1\uacfc\uc801\uc778", "\uc989\uac01\uc801\uc778");
  shape.text = normalizedValue;
  shape.text.style = { typeface: "Apple SD Gothic Neo" };
  return shape;
}

function setNotes(slide, body) {
  const sources = [
    "[Sources]",
    "- 시각 템플릿 맟 레이아웃: 사용자 제공 Keynote 5종",
    "- 주 레이아웃: 심연의청강단_게임컨셉발표.key",
  ].join("\n");
  const normalizedNotes = `${body}\n\n${sources}`
    .normalize("NFC")
    .replaceAll("\uad6c\uce59", "\uaddc\uce59")
    .replaceAll("\uc9c1\uacfc\uc801\uc778", "\uc989\uac01\uc801\uc778");
  slide.speakerNotes.textFrame.setText(normalizedNotes);
  slide.speakerNotes.setVisible(true);
}

async function main() {
  await fs.mkdir(RENDER_DIR, { recursive: true });
  await fs.mkdir(LAYOUT_DIR, { recursive: true });
  await fs.mkdir(path.dirname(OUTPUT), { recursive: true });

  const { FileBlob, PresentationFile } = await importRuntimeModule("@oai/artifact-tool");
  const presentation = await PresentationFile.importPptx(await FileBlob.load(STARTER));
  const snapshot = await presentation.inspect({ kind: "slide,textbox,shape", maxChars: 100000 });
  const textIndex = makeTextIndex(snapshot);

  // 1. Opening
  const openingTitle = setText(presentation, textIndex, 1, "심연의 청강단", "게임은 무엇인가");
  openingTitle.text.style = { color: "#D124A6" };
  setText(presentation, textIndex, 1, "최용근이 만든 빌드 파일 발표 및 시연", "구칙 안에서 선택하고, 결과를 경험하는 매체");
  setNotes(
    presentation.slides.getItem(0),
    "게임을 단순히 보거나 듣는 것이 아니라, 플레이어가 직접 선택하고 결과를 받는 매체라는 점을 던진다."
  );

  // 2. Overview
  setText(presentation, textIndex, 2, "LUNA : 사이버펑크 바텐더", "GAME : 참여의 매체");
  setText(presentation, textIndex, 2, "게임 개요", "");
  setText(
    presentation,
    textIndex,
    2,
    "장르\n플랫폼\n개발 엔진\n아트 풍\n레퍼런스 게임",
    "출발점\n플레이어\n핵심 행동\n작동 방식\n남는 것"
  );
  setText(
    presentation,
    textIndex,
    2,
    "바텐더",
    "주어진 상황과 목표\n선택하고 행동하는 사람\n판단 · 실행 · 반복\n구칙과 직과적인 피드백\n나만의 경험과 이야기"
  );
  setNotes(
    presentation.slides.getItem(1),
    "게임을 구성하는 핵심은 플레이어가 판단하고 시스템이 반응하는 경험이다. 핵심은 구칙을 만들고, 결과적으로 자신의 이야기를 만든다."
  );

  // 3. Model
  setText(presentation, textIndex, 3, "바텐더의 경험?", "게임의 세 요소");
  setText(
    presentation,
    textIndex,
    3,
    "본 프로젝트",
    "게임은 구칙만으로도, 이야기만으로도 완성되지 않는다.\n세 요소가 서로 반응할 때 플레이가 된다."
  );
  setText(presentation, textIndex, 3, "바텐더", "게임");
  setText(presentation, textIndex, 3, "손님과 소통", "구칙");
  setText(presentation, textIndex, 3, "술 만들기", "선택");
  setText(presentation, textIndex, 3, "매장 관리", "피드백");
  setNotes(
    presentation.slides.getItem(2),
    "구칙은 가능한 행동을 제한하고, 선택은 의미 있는 판단을 고르며, 피드백은 결과를 나타낸다. 이 세 가지가 이어질 때 게임이 된다."
  );

  // 4. Experience design
  setText(presentation, textIndex, 4, "술 만들기", "경험 설계");
  setText(presentation, textIndex, 4, "바텐더의 경험", "플레이의 의미");
  setText(presentation, textIndex, 4, "마스터, 나한테 맞는 한잔으로.", "무엇을 할 수 있을까?");
  setText(presentation, textIndex, 4, "오늘 고된 하루를 날려보낼만한 걸로 부탁해.", "어떤 선택이 나의 판단이 될까?");
  setText(presentation, textIndex, 4, "주문 바ᄃ았습니다.", "결과가 달라졌다.");
  setText(presentation, textIndex, 4, "1. 주문받기", "선택과 결과");
  const experienceBody = setText(
    presentation,
    textIndex,
    4,
    "바텐더는",
    "플레이어는 구칙 안에서 상황을 읽고\n자신의 판단으로 행동한다.\n\n게임은 그 행동에 결과를 돌려주며,\n선택이 세계를 바꾼다는 감각을 만든다."
  );
  experienceBody.position = { ...experienceBody.position, top: experienceBody.position.top + 58 };
  setNotes(
    presentation.slides.getItem(3),
    "게임 디자인의 핵심은 플레이어가 반복할 정답을 맞히는 것이다. 구경하는 판단을 제공하고, 다야한 결과를 학습하게 연결시키는 구조이다."
  );

  // 5. Closing
  const closingTitle = setText(presentation, textIndex, 5, "심연의 청강단", "게임은 선택의 경험이다");
  closingTitle.text.style = { color: "#D124A6" };
  setText(presentation, textIndex, 5, "최용근이 만든 빌드 파일 발표 및 시연", "플레이어가 행동한 만큼, 세계는 자신의 이야기가 된다.");
  setNotes(
    presentation.slides.getItem(4),
    "게임은 작자가 정해진 이야기를 읽는 것이 아니라, 플레이어가 선택한 의미와 결과가 경험을 완성한다고 마무리한다."
  );

  for (const [index, slide] of presentation.slides.items.entries()) {
    const stem = `slide-${String(index + 1).padStart(2, "0")}`;
    const png = await presentation.export({ slide, format: "png", scale: 2 });
    await writeBlob(path.join(RENDER_DIR, `${stem}.png`), png);
    const layout = await slide.export({ format: "layout" });
    await fs.writeFile(path.join(LAYOUT_DIR, `${stem}.layout.json`), await layout.text());
  }

  const montage = await presentation.export({ format: "webp", montage: true, scale: 1 });
  await writeBlob(MONTAGE, montage);

  const pptx = await PresentationFile.exportPptx(presentation);
  await pptx.save(OUTPUT);

  console.log(JSON.stringify({ output: OUTPUT, renders: RENDER_DIR, layouts: LAYOUT_DIR, montage: MONTAGE }, null, 2));
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
