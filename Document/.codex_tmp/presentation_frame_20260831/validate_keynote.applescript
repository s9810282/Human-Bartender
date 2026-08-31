set sourcePath to "/private/tmp/Project_LUNA_2026_하반기_발표자료_프레임_noto.key"
set outputPath to "/Users/lee/Desktop/클로드/Human-Bartender/Document/.codex_tmp/presentation_frame_20260831/keynote-validation-noto.pdf"

tell application "Keynote Creator Studio"
    activate
    set sourceAlias to POSIX file sourcePath as alias
    set docRef to open sourceAlias
    delay 3
    export docRef to POSIX file outputPath as PDF
    delay 2
    close docRef saving no
end tell
