set sourceFiles to {¬
	"/Users/lee/Desktop/졸업작품 폴더/발표자료/심연의청강단_게임컨셉발표.key", ¬
	"/Users/lee/Desktop/졸업작품 폴더/발표자료/심연의청강단_라운드테이블.key", ¬
	"/Users/lee/Desktop/졸업작품 폴더/발표자료/심연의청강단_중간발표.key", ¬
	"/Users/lee/Desktop/졸업작품 폴더/발표자료/심연의청강단_프로토타입.key", ¬
	"/Users/lee/Desktop/졸업작품 폴더/발표자료/1234.key"}

set outputDir to "/Users/lee/Desktop/클로드/Human-Bartender/Document/.codex_tmp/keynote_style_20260831/exports/"

tell application "Keynote"
	activate
	repeat with i from 1 to count of sourceFiles
		set sourcePath to item i of sourceFiles
		set sourceAlias to POSIX file sourcePath as alias
		set docRef to open sourceAlias
		delay 1
		set baseName to "reference_" & i
		export docRef to POSIX file (outputDir & baseName & ".pdf") as PDF
		export docRef to POSIX file (outputDir & baseName & ".pptx") as Microsoft PowerPoint
		close docRef saving no
	end repeat
end tell
