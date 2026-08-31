import fs from "node:fs/promises";
import path from "node:path";
import { importRuntimeModule } from "/Users/lee/.codex/plugins/cache/openai-primary-runtime/presentations/26.826.12353/skills/presentations/container_tools/runtime_helpers.mjs";

const ROOT = "/Users/lee/Desktop/클로드/Human-Bartender/Document/.codex_tmp/presentation_frame_20260831";
const STARTER = path.join(ROOT, "presentation-frame-starter.pptx");
const RENDER_DIR = path.join(ROOT, "final-render");
const LAYOUT_DIR = path.join(ROOT, "final-layout");
const MONTAGE = path.join(ROOT, "final-montage.webp");
const OUTPUT = "/Users/lee/Desktop/클로드/Human-Bartender/Document/Generated/Project_LUNA_2026_하반기_발표자료_프레임.pptx";
const FONT = "Noto Sans CJK KR";
const MAGENTA = "#D124A6";
const CYAN = "#44D9E6";
const WHITE = "#F5F5F5";
const MUTED = "#AEB2B8";

async function writeBlob(filePath, blob) {
  await fs.writeFile(filePath, new Uint8Array(await blob.arrayBuffer()));
}

function buildIndex(snapshot) {
  const texts = new Map();
  const images = new Map();
  const shapes = new Map();
  for (const line of snapshot.ndjson.split("\n")) {
    if (!line.trim()) continue;
    const item = JSON.parse(line);
    if ((item.kind === "textbox" || item.kind === "shape") && item.text && item.id && item.slide) {
      const key = `${item.slide}\u0000${item.text.normalize("NFC")}`;
      if (!texts.has(key)) texts.set(key, []);
      texts.get(key).push(item.id);
    }
    if (item.kind === "image" && item.id && item.slide) {
      if (!images.has(item.slide)) images.set(item.slide, []);
      images.get(item.slide).push(item.id);
    }
    if (item.kind === "shape" && !item.text && item.id && item.slide) {
      if (!shapes.has(item.slide)) shapes.set(item.slide, []);
      shapes.get(item.slide).push(item.id);
    }
  }
  return { texts, images, shapes, used: new Set() };
}

function resolveText(presentation, index, slideNumber, originalText) {
  const key = `${slideNumber}\u0000${originalText.normalize("NFC")}`;
  const ids = index.texts.get(key) || [];
  const id = ids.find((candidate) => !index.used.has(candidate));
  if (!id) throw new Error(`Text not found on slide ${slideNumber}: ${originalText}`);
  index.used.add(id);
  return presentation.resolve(id);
}

function setText(presentation, index, slideNumber, originalText, nextText, style = {}, position) {
  const shape = resolveText(presentation, index, slideNumber, originalText);
  shape.text = nextText.normalize("NFC");
  shape.text.style = { typeface: FONT, ...style };
  if (position) shape.position = position;
  return shape;
}

function note(slide, purpose) {
  slide.speakerNotes.textFrame.setText(
    `${purpose}\n\n[Sources]\n- 발표 구성: https://app.notion.com/p/3cd1612298dc80bdb1fec50672df9810\n- 시각 참고: 사용자가 제공한 기존 Keynote 발표자료 5종(색상·폰트 인상·대제목 위치만 참고)\n- 폰트: Noto Sans CJK KR, https://github.com/notofonts/noto-cjk`.normalize("NFC"),
  );
  slide.speakerNotes.setVisible(true);
}

function removeSlideImages(presentation, index, slideNumber) {
  for (const id of index.images.get(slideNumber) || []) presentation.resolve(id).delete();
}

function styleCover(presentation, index, slideNumber, title, subtitle, titleSource = "심연의 청강단") {
  const accentId = (index.shapes.get(slideNumber) || [])[0];
  if (accentId) presentation.resolve(accentId).fill = MAGENTA;
  setText(presentation, index, slideNumber, titleSource, title, {
    fontSize: 72, bold: true, color: WHITE, alignment: "center",
  }, { left: 380, top: 490, width: 1800, height: 190 });
  setText(presentation, index, slideNumber, "최용근이 만든 빌드 파일 발표 및 시연", subtitle, {
    fontSize: 30, color: MUTED, alignment: "center",
  }, { left: 430, top: 700, width: 1700, height: 110 });
}

function styleOverview(presentation, index, slideNumber, title, eyebrow, labels, values) {
  setText(presentation, index, slideNumber, "LUNA : 사이버펑크 바텐더", title, {
    fontSize: 52, bold: true, color: WHITE, alignment: "center",
  }, { left: 500, top: 250, width: 1560, height: 150 });
  setText(presentation, index, slideNumber, "게임 개요", eyebrow, {
    fontSize: 24, bold: true, color: MAGENTA, alignment: "center",
  }, { left: 890, top: 105, width: 780, height: 95 });
  setText(presentation, index, slideNumber, "장르\n플랫폼\n개발 엔진\n아트 풍\n레퍼런스 게임", labels.join("\n"), {
    fontSize: 26, bold: true, color: WHITE,
  }, { left: 720, top: 610, width: 510, height: 470 });
  setText(presentation, index, slideNumber, "바텐더 시뮬레이션 어드벤쳐\nPC\nUnity 6\n픽셀\nVa-11 Hall-a, Coffee Talk", values.join("\n"), {
    fontSize: 25, color: MUTED,
  }, { left: 1330, top: 610, width: 630, height: 470 });
}

function styleThreePart(presentation, index, slideNumber, title, intro, center, left, middle, right) {
  setText(presentation, index, slideNumber, "바텐더의 경험?", title, {
    fontSize: 50, bold: true, color: WHITE, alignment: "center",
  }, { left: 650, top: 70, width: 1260, height: 150 });
  setText(presentation, index, slideNumber, "본 프로젝트는 플레이어가 경험했으면 하는\n 바텐더의 경험을 총 3가지로 나눠서 전달한다.", intro, {
    fontSize: 26, color: MUTED, alignment: "center",
  }, { left: 690, top: 230, width: 1180, height: 140 });
  setText(presentation, index, slideNumber, "바텐더", center, {
    fontSize: 32, bold: true, color: WHITE, alignment: "center",
  });
  setText(presentation, index, slideNumber, "손님과 소통", left, {
    fontSize: 26, bold: true, color: WHITE, alignment: "center",
  });
  setText(presentation, index, slideNumber, "술 만들기", middle, {
    fontSize: 26, bold: true, color: WHITE, alignment: "center",
  });
  setText(presentation, index, slideNumber, "매장 관리", right, {
    fontSize: 26, bold: true, color: WHITE, alignment: "center",
  });
}

function styleDetail(presentation, index, slideNumber, title, eyebrow, subhead, mediaLabel, point1, point2, body) {
  removeSlideImages(presentation, index, slideNumber);
  setText(presentation, index, slideNumber, "술 만들기", title, {
    fontSize: 46, bold: true, color: WHITE,
  }, { left: 105, top: 58, width: 1280, height: 120 });
  setText(presentation, index, slideNumber, "바텐더의 경험", eyebrow, {
    fontSize: 22, bold: true, color: MAGENTA, alignment: "right",
  }, { left: 1800, top: 88, width: 650, height: 82 });
  setText(presentation, index, slideNumber, "1. 주문받기", subhead, {
    fontSize: 34, bold: true, color: WHITE,
  }, { left: 1000, top: 245, width: 850, height: 100 });
  setText(presentation, index, slideNumber, "마스터, 나한테 맞는 한잔으로.", mediaLabel, {
    fontSize: 25, bold: true, color: CYAN, alignment: "center",
  }, { left: 765, top: 590, width: 350, height: 80 });
  setText(presentation, index, slideNumber, "오늘 고된 하루를 날려보낼만한 걸로 부탁해.", point1, {
    fontSize: 23, color: WHITE,
  }, { left: 1180, top: 465, width: 600, height: 72 });
  setText(presentation, index, slideNumber, "주문 받았습니다.", point2, {
    fontSize: 22, color: WHITE,
  }, { left: 1540, top: 675, width: 330, height: 70 });
  setText(presentation, index, slideNumber, "바텐더는 손님들에게 주문을 받지만 \n손님들이 항상 정해진 메뉴만 요청하는 건 아니다.\n\n손님의 기분, 숨겨진 요청, 그리고 지난날의 대화와 기억까지.\n모든 맥락을 읽어내어 그들이 진정 어떤 술이 필요한지 찾아야한다.", body, {
    fontSize: 27, color: MUTED,
  }, { left: 655, top: 930, width: 1250, height: 275 });
}

async function main() {
  await fs.mkdir(RENDER_DIR, { recursive: true });
  await fs.mkdir(LAYOUT_DIR, { recursive: true });
  await fs.mkdir(path.dirname(OUTPUT), { recursive: true });

  const { FileBlob, PresentationFile } = await importRuntimeModule("@oai/artifact-tool");
  const presentation = await PresentationFile.importPptx(await FileBlob.load(STARTER));
  const snapshot = await presentation.inspect({ kind: "slide,textbox,shape,image", maxChars: 300000 });
  const index = buildIndex(snapshot);

  styleCover(presentation, index, 1, "PROJECT L.U.N.A", "여름방학 개발 성과 및 2학기 제작 계획");
  note(presentation.slides.getItem(0), "발표 표지. 발표명·날짜·발표자를 필요에 맞게 교체한다.");

  styleOverview(presentation, index, 2, "프로젝트 현황 한눈에 보기", "PROJECT SNAPSHOT",
    ["현재 개발 초점", "데모 범위", "시스템 개편", "다음 빌드 목표", "외부 준비"],
    ["0일차 전체 루프 · 빌드 검증", "Day 0–3 · 총 4일", "바 · 제조 · 거리 · 컷씬", "0일차 시작부터 종료까지", "Steam · Next Fest · 텀블벅 · 플레이 데이터"]);
  note(presentation.slides.getItem(1), "노션의 프로젝트 현황 요약을 한 장에 보여주는 슬라이드.");

  styleCover(presentation, index, 3, "방학 중 진행사항", "그래픽 · 시스템 · 출시 준비", "심연의 청강단");
  note(presentation.slides.getItem(2), "첫 번째 대목을 여는 섹션 구분 슬라이드.");

  styleThreePart(presentation, index, 4, "그래픽 리소스 관련",
    "세 영역의 결과물을 한 장에서 요약하고, 다음 장부터 각 영역을 자세히 보여준다.",
    "GRAPHIC", "바 내부·캐릭터", "칵테일·기믹", "거리·컷씬");
  note(presentation.slides.getItem(3), "그래픽 리소스 관련 세부 내용을 세 갈래로 나누는 개요 슬라이드.");

  styleDetail(presentation, index, 5, "바 내부 화면 및 캐릭터 리소스", "GRAPHIC / BAR",
    "현재 화면과 변경점을 함께 보여주세요", "[대표 이미지 또는 캡처]", "핵심 변경점 01", "핵심 변경점 02",
    "• 무엇을 제작하거나 수정했는지 입력\n• 실제 플레이 화면에서 달라진 점 입력\n• 다음 단계 또는 남은 작업 입력");
  note(presentation.slides.getItem(4), "바 내부 화면과 캐릭터 리소스의 대표 이미지를 넣는 상세 프레임.");

  styleDetail(presentation, index, 6, "칵테일 제조 화면 및 기믹 리소스", "GRAPHIC / CRAFT",
    "화면별 결과물과 기믹별 차이를 보여주세요", "[기믹 화면 또는 리소스]", "제조 화면", "입력·피드백",
    "• 재료 선택·제조·완성 화면의 변화 입력\n• 따르기·병따기·셰이킹·스터 등 주요 기믹 입력\n• 추가 제작이 필요한 리소스 입력");
  note(presentation.slides.getItem(5), "칵테일 제조와 기믹 리소스를 설명하는 상세 프레임.");

  styleDetail(presentation, index, 7, "외부 거리 및 컷씬 연출", "GRAPHIC / DIRECTION",
    "배경·캐릭터·연출을 한 장면으로 보여주세요", "[거리 또는 컷씬 캡처]", "공간 연출", "컷씬 연출",
    "• 외부 거리의 화면 구성과 상호작용 표현 입력\n• 연구소 컷씬 또는 타임라인 연출 사례 입력\n• 분위기·카메라·이펙트의 목표 입력");
  note(presentation.slides.getItem(6), "외부 거리와 컷씬 연출 결과를 설명하는 상세 프레임.");

  styleThreePart(presentation, index, 8, "핵심 시스템 개편",
    "플레이 흐름을 구성하는 세 시스템이 어떻게 달라졌는지 요약한다.",
    "SYSTEM", "바 운영 루프", "거리 상호작용", "컷씬 제작 도구");
  note(presentation.slides.getItem(7), "핵심 시스템 개편 세 영역을 소개하는 개요 슬라이드.");

  styleDetail(presentation, index, 9, "바 운영 루프 개편", "SYSTEM / BAR LOOP",
    "이전 흐름과 현재 흐름을 비교하세요", "[플레이 흐름 다이어그램]", "1부 일반 손님", "2부 단골 손님",
    "• 손님 등장부터 주문·제조·서빙까지의 흐름 입력\n• 1부와 2부의 차이와 연결 방식 입력\n• 실제 플레이에서 해결한 문제 입력");
  note(presentation.slides.getItem(8), "바 운영 1부·2부의 개편 내용을 설명하는 상세 프레임.");

  styleDetail(presentation, index, 10, "외부 거리 상호작용 시스템", "SYSTEM / STREET",
    "접근·상호작용·대사 흐름을 보여주세요", "[거리 시스템 캡처]", "직접 상호작용", "자동 방송",
    "• NPC와 오브젝트가 등장하는 조건 입력\n• E 상호작용과 말풍선 진행 방식 입력\n• Day 99 테스트에서 확인한 항목 입력");
  note(presentation.slides.getItem(9), "외부 거리 상호작용 시스템의 데이터 흐름과 화면을 설명하는 상세 프레임.");

  styleDetail(presentation, index, 11, "타임라인 기반 컷씬 제작 도구", "SYSTEM / CUTSCENE",
    "작업 화면과 결과 장면을 함께 보여주세요", "[Unity 타임라인 캡처]", "제작 과정", "재생 결과",
    "• 타임라인에서 직접 조립할 수 있는 기능 입력\n• 대사·카메라·애니메이션·이펙트 호출 방식 입력\n• 초보 작업자를 위한 편의 기능 입력");
  note(presentation.slides.getItem(10), "타임라인 컷씬 제작 도구의 작업 화면과 결과를 설명하는 상세 프레임.");

  styleThreePart(presentation, index, 12, "출시·홍보 및 플레이테스트 준비",
    "외부 공개와 내부 검증을 동시에 준비한 과정을 세 영역으로 나눈다.",
    "PREP", "텀블벅", "Steam·Next Fest", "플레이 데이터");
  note(presentation.slides.getItem(11), "출시·홍보·플레이테스트 준비를 세 갈래로 소개하는 개요 슬라이드.");

  styleOverview(presentation, index, 13, "출시와 검증 준비", "LAUNCH & TEST",
    ["텀블벅", "Steam 상점", "Next Fest", "플레이테스트", "데이터 수집"],
    ["진행 내용 또는 목표 입력", "공개 상태와 보완점 입력", "참가 준비와 일정 입력", "테스트 방식과 대상 입력", "수집 항목과 활용 계획 입력"]);
  note(presentation.slides.getItem(12), "출시와 검증 준비 내용을 한눈에 정리하는 체크리스트형 슬라이드.");

  styleDetail(presentation, index, 14, "2학기 계획 및 마일스톤", "ROADMAP / SEMESTER 2",
    "월별 핵심 결과물과 검증 시점을 입력하세요", "9월", "10월", "11월 이후",
    "• 월별 목표와 완료 기준 입력\n• 빌드·전시·테스트 일정 입력\n• 일정 사이의 의존 관계와 위험 요소 입력");
  note(presentation.slides.getItem(13), "2학기 계획과 마일스톤을 월별로 채우는 타임라인 프레임.");

  styleOverview(presentation, index, 15, "팀원 변동 사항 및 현재 구성 현황", "TEAM",
    ["이전 구성", "현재 구성", "기획", "프로그래밍", "그래픽"],
    ["8명", "11명", "이름 · 역할 · 담당 입력", "이름 · 역할 · 담당 입력", "이름 · 역할 · 담당 입력"]);
  note(presentation.slides.getItem(14), "팀원 변동과 현재 역할 분담을 정리하는 슬라이드.");

  styleCover(presentation, index, 16, "다음 빌드 목표", "0일차 시작부터 종료까지 이어지는 플레이 가능한 빌드");
  note(presentation.slides.getItem(15), "발표 마무리. 다음 빌드 목표와 요청 사항을 한 문장으로 제시한다.");

  // Enforce the requested cross-platform font on every visible text object.
  const after = await presentation.inspect({ kind: "textbox,shape", maxChars: 300000 });
  for (const line of after.ndjson.split("\n")) {
    if (!line.trim()) continue;
    const item = JSON.parse(line);
    if ((item.kind === "textbox" || item.kind === "shape") && item.text && item.id) {
      presentation.resolve(item.id).text.style = { typeface: FONT };
    }
  }

  for (const [i, slide] of presentation.slides.items.entries()) {
    const stem = `slide-${String(i + 1).padStart(2, "0")}`;
    await writeBlob(path.join(RENDER_DIR, `${stem}.png`), await presentation.export({ slide, format: "png", scale: 2 }));
    await writeBlob(path.join(LAYOUT_DIR, `${stem}.layout.json`), await slide.export({ format: "layout" }));
  }
  await writeBlob(MONTAGE, await presentation.export({ format: "webp", montage: true, scale: 1 }));
  await (await PresentationFile.exportPptx(presentation)).save(OUTPUT);
  console.log(JSON.stringify({ output: OUTPUT, renderDir: RENDER_DIR, layoutDir: LAYOUT_DIR, montage: MONTAGE }, null, 2));
}

main().catch((error) => { console.error(error); process.exitCode = 1; });
