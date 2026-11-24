import QtQuick 2.12
import Lomiri.Components 1.3

// Disposable native Qt control. Run only after copying the known test value 52.
MainView {
    applicationName: "calculator.clipboardprobe"
    width: units.gu(45)
    height: units.gu(80)
    TextArea { id: editor; anchors.fill: parent }
    Timer {
        interval: 1500; running: true; repeat: false
        onTriggered: { editor.forceActiveFocus(); editor.paste(); verify.start(); }
    }
    Timer {
        id: verify; interval: 1000
        onTriggered: {
            console.log("CALCULATOR_TO_QT=" + (editor.text === "52"));
            editor.text = "314.159"; editor.selectAll(); editor.copy();
            finish.start();
        }
    }
    Timer { id: finish; interval: 1000; onTriggered: Qt.quit() }
}
