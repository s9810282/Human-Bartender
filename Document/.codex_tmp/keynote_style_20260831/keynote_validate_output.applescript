set sourcePath to "/Users/lee/Desktop/클로드/Human-Bartender/Document/Generated/게임은_무엇인가_5장_발표자료.pptx"
set outputPath to "/Users/lee/Desktop/클로드/Human-Bartender/Document/.codex_tmp/keynote_style_20260831/keynote_validation/fixed_deck.pdf"

tell application "Finder"
	set outputFolder to POSIX file "/Users/lee/Desktop/클로드/Human-Bartender/Document/.codex_tmp/keynote_style_20260831/keynote_validation/"
	if not (exists outputFolder) then make new folder at POSIX file "/Users/lee/Desktop/클로드/Human-Bartender/Document/.codex_tmp/keynote_style_20260831/" with properties {name:"keynote_validation"}
end tell

tell application "Keynote Creator Studio"
	activate
	set sourceAlias to POSIX file sourcePath as alias
	set docRef to open sourceAlias
	delay 3
	export docRef to POSIX file outputPath as PDF
	delay 2
	close docRef saving no
end tell
