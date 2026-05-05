import sys

from PySide6.QtWidgets import QApplication

from .main_window import MainWindow
from .theme import APP_STYLESHEET


def main() -> int:
    app = QApplication(sys.argv)
    app.setApplicationName("ContourViewer")
    app.setOrganizationName("Staad Tools")
    app.setStyleSheet(APP_STYLESHEET)

    window = MainWindow()
    window.show()

    return app.exec()


if __name__ == "__main__":
    raise SystemExit(main())
