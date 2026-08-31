on run argv
    set sourcePath to item 1 of argv
    set outputPath to item 2 of argv
    tell application "Keynote Creator Studio"
        activate
        set sourceAlias to POSIX file sourcePath as alias
        set docRef to open sourceAlias
        delay 3
        set outputFile to (POSIX file outputPath) as text
        save docRef in file outputFile
        delay 2
        close docRef saving no
    end tell
end run
