import fs from "node:fs/promises";

const malePath = "/Users/lee/Desktop/클로드/junseo874.github.io/guest_parts_sim.html";
const femalePath = "/Users/lee/Desktop/클로드/작업물/여자_일반손님_파츠_시뮬레이터.html";

function extractParts(html, label, gender) {
  const singleMatch = html.match(/const PARTS = (\{[\s\S]*?\});\n\nconst CATS/);
  if (singleMatch) return JSON.parse(singleMatch[1]);

  const combinedMatch = html.match(/const PARTS_BY_GENDER = (\{[\s\S]*?\});\n\nconst GENDERS/);
  if (combinedMatch) {
    const groups = JSON.parse(combinedMatch[1]);
    if (groups[gender]) return groups[gender];
  }

  throw new Error(`${label} PARTS 데이터를 찾지 못했습니다.`);
}

const maleParts = extractParts(await fs.readFile(malePath, "utf8"), "남자", "male");
const femaleParts = extractParts(await fs.readFile(femalePath, "utf8"), "여자", "female");

const html = `<!DOCTYPE html>
<html lang="ko">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>랜덤 손님 파츠 조합 시뮬레이터 (남자·여자)</title>
<style>
  :root{
    --bg:#08090d; --panel:#111319; --panel-2:#171a22; --line:#262b38;
    --gold:#f2c14e; --text:#e9ebf2; --text-dim:#8890a4;
    --good:#38d47a; --bad:#ff5c6c; --cyan:#7ff0ea; --purple:#8f86e8;
  }
  *{box-sizing:border-box;}
  body{
    margin:0; background:var(--bg); color:var(--text);
    font-family:"Pretendard","Malgun Gothic","Noto Sans KR",sans-serif;
    min-height:100vh; display:flex; justify-content:center;
    padding:24px 16px 60px;
  }
  .app{width:100%;max-width:1180px;}
  h1{font-size:20px;font-weight:700;margin:0 0 4px;letter-spacing:-0.01em;}
  .sub{color:var(--text-dim);font-size:13px;margin:0 0 12px;line-height:1.7;}
  .sub b{color:var(--gold);font-weight:600;}
  .gender-switch{
    display:inline-flex; gap:6px; padding:4px; margin:0 0 16px;
    border:1px solid var(--line); border-radius:10px; background:var(--panel);
  }
  .gender-switch button{
    flex:none; min-width:92px; padding:8px 16px; border-color:transparent;
    background:transparent; color:var(--text-dim);
  }
  .gender-switch button:hover{border-color:var(--cyan); color:var(--cyan);}
  .gender-switch button.active{
    background:var(--gold); border-color:var(--gold); color:#1b1405;
  }
  .rule-note{
    display:none; margin:-6px 0 14px; color:var(--bad); font-size:12px; line-height:1.6;
  }
  .rule-note.show{display:block;}
  .layout{display:flex; gap:16px; align-items:flex-start;}

  .preview{
    flex:0 0 470px; position:sticky; top:16px;
    background:var(--panel); border:1px solid var(--line); border-radius:14px;
    padding:16px; display:flex; flex-direction:column; gap:12px;
  }
  .stageWrap{
    min-height:500px;
    background:radial-gradient(ellipse 70% 55% at 50% 40%, #182030 0%, #10141c 75%),#10141c;
    border:1px solid var(--line); border-radius:10px;
    display:flex; align-items:center; justify-content:center; padding:10px;
    overflow:hidden;
  }
  #stage{display:block;image-rendering:pixelated;}
  .btnRow{display:flex;gap:8px;}
  button{
    flex:1; background:var(--panel-2); border:1px solid var(--line); color:var(--text);
    border-radius:8px; padding:10px 0; font-size:14px; font-weight:700;
    font-family:inherit; cursor:pointer;
  }
  button:hover{border-color:var(--cyan);color:var(--cyan);}
  button.primary{background:var(--gold);border-color:var(--gold);color:#1b1405;}
  button.primary:hover{filter:brightness(1.08);color:#1b1405;}
  .combo{
    background:var(--panel-2); border:1px solid var(--line); border-radius:8px;
    padding:10px 12px; font-size:12px; line-height:1.8; color:var(--text-dim);
  }
  .combo b{color:var(--text);font-weight:600;}
  .combo .cat{color:var(--purple);font-weight:700;}

  .parts{flex:1;display:flex;flex-direction:column;gap:12px;min-width:0;}
  .cat-card{background:var(--panel);border:1px solid var(--line);border-radius:14px;padding:12px 14px 14px;}
  .cat-head{display:flex;align-items:baseline;gap:8px;margin-bottom:10px;}
  .cat-name{font-size:14px;font-weight:800;}
  .cat-count{font-size:11px;color:var(--text-dim);}
  .cat-layer{margin-left:auto;font-size:11px;color:var(--purple);font-weight:700;}
  .thumbs{display:flex;flex-wrap:wrap;gap:8px;}
  .thumb{
    position:relative; border:2px solid var(--line); border-radius:8px;
    background:#10141c; cursor:pointer; padding:4px 4px 18px;
    transition:border-color 120ms,box-shadow 120ms;
  }
  .thumb:hover{border-color:var(--cyan);}
  .thumb.sel{border-color:var(--gold);box-shadow:0 0 10px rgba(242,193,78,.25);}
  .thumb.blocked{opacity:.32;cursor:not-allowed;filter:grayscale(1);}
  .thumb.blocked:hover{border-color:var(--line);}
  .thumb canvas{display:block;image-rendering:pixelated;}
  .thumb .tname{
    position:absolute;left:0;right:0;bottom:3px;text-align:center;
    font-size:10px;color:var(--text-dim);white-space:nowrap;
  }
  .thumb.sel .tname{color:var(--gold);}
  .thumb.none-item{display:flex;align-items:center;justify-content:center;min-width:72px;min-height:72px;}
  .thumb.none-item span{font-size:12px;color:var(--text-dim);font-weight:700;margin-bottom:14px;}

  @media (max-width: 930px){
    .layout{flex-direction:column;}
    .preview{position:static;flex:none;width:100%;}
    .parts{width:100%;}
  }
  @media (max-width: 520px){
    .preview{padding:12px;}
    .stageWrap{min-height:390px;}
    #stage{max-width:100%;height:auto!important;}
  }
</style>
</head>
<body>
<div class="app">
  <h1>랜덤 손님 파츠 조합 시뮬레이터 — 남자·여자</h1>
  <p class="sub">
    성별을 선택하면 해당 파츠 목록과 미리보기가 함께 전환된다.
    레이어 순서: <b>바디 → 의상/상의 → 액세서리 → 눈썹 → 눈 → 입 → 헤어</b> ·
    현재 성별 조합 <b id="comboTotal">-</b>종 · 선택 상태는 성별마다 따로 유지된다.
  </p>
  <div class="gender-switch" role="group" aria-label="손님 성별 선택">
    <button type="button" class="gender-btn active" data-gender="female" aria-pressed="true">여자</button>
    <button type="button" class="gender-btn" data-gender="male" aria-pressed="false">남자</button>
  </div>
  <p class="rule-note" id="ruleNote">남자 금지 조합: 팔목 밴드(Acc_1)는 민소매 의상 Shirt_3 또는 Shirt_3(2)와만 조합할 수 있습니다.</p>

  <div class="layout">
    <div class="preview">
      <div class="stageWrap"><canvas id="stage" width="210" height="321"></canvas></div>
      <div class="btnRow">
        <button class="primary" id="btnRandom">🎲 랜덤 생성</button>
        <button id="btnSave">PNG 저장</button>
      </div>
      <div class="combo" id="comboReadout"></div>
    </div>
    <div class="parts" id="partsPanel"></div>
  </div>
</div>

<script>
"use strict";
const PARTS_BY_GENDER = ${JSON.stringify({ female: femaleParts, male: maleParts })};

const GENDERS = {
  female: {
    label:"여자", width:210, height:321, displayHeight:470,
    cats:[
      {key:"body",name:"바디",layer:1,items:["body"],fixed:true},
      {key:"top",name:"상의",layer:2,items:["top_1","top_2","top_3"]},
      {key:"acc",name:"액세서리",layer:3,items:[null,"Acc_1","Acc_2","Acc_3"],labels:{"Acc_1":"팔 액세서리","Acc_2":"목 액세서리","Acc_3":"아우터"}},
      {key:"eyebrow",name:"눈썹",layer:4,items:["Eyebrow_1","Eyebrow_2","Eyebrow_3"]},
      {key:"eye",name:"눈",layer:5,items:["Eye_1","Eye_2","Eye_3"]},
      {key:"mouth",name:"입",layer:6,items:["mouth_1","mouth_2","mouth_3"]},
      {key:"hair",name:"헤어",layer:7,items:["Hair_1","Hair_2"]},
    ],
  },
  male: {
    label:"남자", width:332, height:388, displayHeight:470,
    cats:[
      {key:"body",name:"바디",layer:1,items:["body"],fixed:true},
      {key:"shirt",name:"의상",layer:2,items:["Shirt_1","Shirt_1(2)","Shirt_1(3)","Shirt_1(4)","Shirt_1(5)","Shirt_2","Shirt_3","Shirt_3(2)"]},
      {key:"acc",name:"액세서리",layer:3,items:[null,"Acc_1","Acc_2","Acc_3"],labels:{"Acc_1":"팔목 밴드","Acc_2":"어깨 재킷","Acc_3":"목걸이"}},
      {key:"eyebrow",name:"눈썹",layer:4,items:["Eyebrow_1","Eyebrow_2","Eyebrow_3"]},
      {key:"eye",name:"눈",layer:5,items:["Eye_1","Eye_2","Eye_3"]},
      {key:"mouth",name:"입",layer:6,items:["mouth_1","mouth_2","mouth_3"]},
      {key:"hair",name:"헤어",layer:7,items:["Hair_1","Hair_2"]},
    ],
  },
};

const $ = id => document.getElementById(id);
const stage = $("stage");
const ctx = stage.getContext("2d");
const images = {female:{},male:{}};
const states = {female:{},male:{}};
const bboxCache = {female:{},male:{}};
let activeGender = "female";
const MALE_ACC1_ALLOWED_SHIRTS = new Set(["Shirt_3","Shirt_3(2)"]);

let loaded = 0;
const totalImgs = Object.values(PARTS_BY_GENDER).reduce((sum, group) => sum + Object.keys(group).length, 0);
for (const [gender, group] of Object.entries(PARTS_BY_GENDER)) {
  for (const [name, url] of Object.entries(group)) {
    const im = new Image();
    im.onload = () => { if (++loaded === totalImgs) init(); };
    im.onerror = () => { console.error("이미지 로드 실패", gender, name); if (++loaded === totalImgs) init(); };
    im.src = url;
    images[gender][name] = im;
  }
}

function currentConfig(){ return GENDERS[activeGender]; }
function currentCats(){ return currentConfig().cats; }
function currentState(){ return states[activeGender]; }
function currentImages(){ return images[activeGender]; }

function isCombinationAllowed(catKey,item,state=currentState()){
  if(activeGender!=="male")return true;
  if(catKey==="acc"&&item==="Acc_1")return MALE_ACC1_ALLOWED_SHIRTS.has(state.shirt);
  return true;
}

function bboxOf(name){
  if (bboxCache[activeGender][name]) return bboxCache[activeGender][name];
  const im = currentImages()[name];
  const c = document.createElement("canvas");
  c.width = im.width; c.height = im.height;
  const g = c.getContext("2d");
  g.drawImage(im,0,0);
  const d = g.getImageData(0,0,c.width,c.height).data;
  let x0=c.width,y0=c.height,x1=0,y1=0,found=false;
  for(let y=0;y<c.height;y++) for(let x=0;x<c.width;x++){
    if(d[(y*c.width+x)*4+3]>8){
      found=true;
      if(x<x0)x0=x;if(x>x1)x1=x;if(y<y0)y0=y;if(y>y1)y1=y;
    }
  }
  const box = found ? {x:x0,y:y0,w:x1-x0+1,h:y1-y0+1} : {x:0,y:0,w:1,h:1};
  bboxCache[activeGender][name]=box;
  return box;
}

function resizeStage(){
  const cfg=currentConfig();
  stage.width=cfg.width;stage.height=cfg.height;
  const scale=cfg.displayHeight/cfg.height;
  stage.style.width=Math.round(cfg.width*scale)+"px";
  stage.style.height=cfg.displayHeight+"px";
}

function draw(){
  ctx.clearRect(0,0,stage.width,stage.height);
  ctx.imageSmoothingEnabled=false;
  const state=currentState(), imageSet=currentImages();
  currentCats().forEach(cat=>{
    const selected=state[cat.key];
    if(selected&&imageSet[selected])ctx.drawImage(imageSet[selected],0,0);
  });
  updateReadout();
}

function updateReadout(){
  const state=currentState();
  const parts=currentCats().filter(cat=>!cat.fixed).map(cat=>{
    const selected=state[cat.key];
    return '<span class="cat">'+cat.name+'</span> <b>'+(selected===null?'없음':selected)+'</b>';
  });
  $("comboReadout").innerHTML='<b>'+currentConfig().label+'</b> 현재 조합 — '+parts.join(' · ');
  document.querySelectorAll(".thumb").forEach(thumb=>{
    thumb.classList.toggle("sel",String(state[thumb.dataset.cat])===thumb.dataset.item);
    const allowed=isCombinationAllowed(thumb.dataset.cat,thumb.dataset.item,state);
    thumb.classList.toggle("blocked",!allowed);
    thumb.setAttribute("aria-disabled",String(!allowed));
    thumb.title=allowed?"":"현재 선택된 파츠와 조합할 수 없습니다.";
  });
}

function combinationCount(){
  if(activeGender==="male"){
    const cfg=currentConfig();
    const shirts=cfg.cats.find(cat=>cat.key==="shirt").items;
    const accessories=cfg.cats.find(cat=>cat.key==="acc").items;
    let validPairs=0;
    shirts.forEach(shirt=>accessories.forEach(acc=>{
      if(acc!=="Acc_1"||MALE_ACC1_ALLOWED_SHIRTS.has(shirt))validPairs++;
    }));
    return cfg.cats
      .filter(cat=>!cat.fixed&&cat.key!=="shirt"&&cat.key!=="acc")
      .reduce((total,cat)=>total*cat.items.length,validPairs);
  }
  return currentCats().filter(cat=>!cat.fixed).reduce((total,cat)=>total*cat.items.length,1);
}

function randomize(){
  const state=currentState();
  if(activeGender==="male"){
    const cfg=currentConfig();
    const shirts=cfg.cats.find(cat=>cat.key==="shirt").items;
    const accessories=cfg.cats.find(cat=>cat.key==="acc").items;
    const validPairs=[];
    shirts.forEach(shirt=>accessories.forEach(acc=>{
      if(acc!=="Acc_1"||MALE_ACC1_ALLOWED_SHIRTS.has(shirt))validPairs.push({shirt,acc});
    }));
    const pair=validPairs[Math.floor(Math.random()*validPairs.length)];
    state.shirt=pair.shirt;state.acc=pair.acc;
  }
  currentCats().forEach(cat=>{
    if(activeGender==="male"&&(cat.key==="shirt"||cat.key==="acc"))return;
    state[cat.key]=cat.fixed?cat.items[0]:cat.items[Math.floor(Math.random()*cat.items.length)];
  });
  draw();
}

function buildPartsPanel(){
  const panel=$("partsPanel");
  panel.innerHTML="";
  const state=currentState();
  currentCats().forEach(cat=>{
    const card=document.createElement("div");card.className="cat-card";
    const head=document.createElement("div");head.className="cat-head";
    head.innerHTML='<span class="cat-name">'+cat.name+'</span><span class="cat-count">'+cat.items.length+'종'+(cat.fixed?' · 고정':'')+'</span><span class="cat-layer">레이어 '+cat.layer+'</span>';
    card.appendChild(head);
    const thumbs=document.createElement("div");thumbs.className="thumbs";
    cat.items.forEach(item=>{
      const thumb=document.createElement("div");thumb.className="thumb";
      thumb.dataset.cat=cat.key;thumb.dataset.item=String(item);
      if(item===null){
        thumb.classList.add("none-item");thumb.innerHTML="<span>없음</span>";
      }else{
        const im=currentImages()[item],box=bboxOf(item);
        const tc=document.createElement("canvas");
        const scale=Math.min(64/box.w,64/box.h,2);
        tc.width=Math.max(24,Math.round(box.w*scale));tc.height=Math.max(24,Math.round(box.h*scale));
        const g=tc.getContext("2d");g.imageSmoothingEnabled=false;
        g.drawImage(im,box.x,box.y,box.w,box.h,0,0,tc.width,tc.height);
        thumb.appendChild(tc);
        const name=document.createElement("div");name.className="tname";
        name.textContent=cat.labels&&cat.labels[item]?cat.labels[item]:item;
        thumb.appendChild(name);
      }
      thumb.addEventListener("click",()=>{
        if(cat.fixed)return;
        if(!isCombinationAllowed(cat.key,item,state))return;
        state[cat.key]=item;
        if(activeGender==="male"&&cat.key==="shirt"&&state.acc==="Acc_1"&&!MALE_ACC1_ALLOWED_SHIRTS.has(item)){
          state.acc=null;
        }
        draw();
      });
      thumbs.appendChild(thumb);
    });
    card.appendChild(thumbs);panel.appendChild(card);
  });
  $("comboTotal").textContent=combinationCount().toLocaleString();
}

function switchGender(gender){
  if(!GENDERS[gender])return;
  activeGender=gender;
  $("ruleNote").classList.toggle("show",gender==="male");
  document.querySelectorAll(".gender-btn").forEach(button=>{
    const active=button.dataset.gender===gender;
    button.classList.toggle("active",active);
    button.setAttribute("aria-pressed",String(active));
  });
  resizeStage();
  if(Object.keys(currentState()).length===0)randomize();
  buildPartsPanel();draw();
}

function init(){
  document.querySelectorAll(".gender-btn").forEach(button=>button.addEventListener("click",()=>switchGender(button.dataset.gender)));
  switchGender("female");
}

$("btnRandom").addEventListener("click",randomize);
$("btnSave").addEventListener("click",()=>{
  const a=document.createElement("a");
  a.download=(activeGender==="female"?"female_guest_":"male_guest_")+Date.now()+".png";
  a.href=stage.toDataURL("image/png");a.click();
});
window.__dbg={GENDERS,states,switchGender,randomize,draw,isCombinationAllowed,get activeGender(){return activeGender;}};
</script>
</body>
</html>`;

await fs.writeFile(femalePath, html, "utf8");
console.log(femalePath);
