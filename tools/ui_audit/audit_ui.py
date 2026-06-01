from __future__ import annotations

import argparse
import json
import statistics
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageStat

try:
    import cv2
    import numpy as np
except ImportError:  # pragma: no cover - exercised when optional deps are absent
    cv2 = None
    np = None


SUPPORTED_EXTENSIONS = {".png", ".jpg", ".jpeg", ".bmp", ".webp"}


@dataclass
class Bounds:
    left: int
    top: int
    right: int
    bottom: int
    width: int
    height: int


@dataclass
class AuditResult:
    image: str
    width: int
    height: int
    content_bounds: Bounds | None
    edge_margin_warning: bool
    horizontal_line_count: int
    vertical_line_count: int
    row_spacing_variance: float | None
    low_contrast_warning: bool
    dense_region_warning: bool
    notes: list[str]


def iter_images(path: Path) -> Iterable[Path]:
    if path.is_file() and path.suffix.lower() in SUPPORTED_EXTENSIONS:
        yield path
        return

    if path.is_dir():
        for candidate in sorted(path.rglob("*")):
            if candidate.is_file() and candidate.suffix.lower() in SUPPORTED_EXTENSIONS:
                yield candidate


def find_content_bounds(image: Image.Image) -> Bounds | None:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size
    min_x, min_y = width, height
    max_x, max_y = -1, -1

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if a > 12 and max(r, g, b) > 16:
                min_x = min(min_x, x)
                min_y = min(min_y, y)
                max_x = max(max_x, x)
                max_y = max(max_y, y)

    if max_x < 0:
        return None

    return Bounds(
        left=min_x,
        top=min_y,
        right=max_x,
        bottom=max_y,
        width=max_x - min_x + 1,
        height=max_y - min_y + 1,
    )


def analyze_lines(image: Image.Image) -> tuple[int, int, float | None, bool]:
    if cv2 is None or np is None:
        return 0, 0, None, False

    gray = np.array(image.convert("L"))
    edges = cv2.Canny(gray, 40, 120)
    lines = cv2.HoughLinesP(edges, 1, np.pi / 180, threshold=48, minLineLength=24, maxLineGap=4)

    horizontal_y: list[int] = []
    vertical_x: list[int] = []

    if lines is not None:
        for line in lines[:, 0]:
            x1, y1, x2, y2 = map(int, line)
            dx = abs(x2 - x1)
            dy = abs(y2 - y1)
            if dx >= 24 and dy <= 2:
                horizontal_y.append(round((y1 + y2) / 2))
            elif dy >= 18 and dx <= 2:
                vertical_x.append(round((x1 + x2) / 2))

    horizontal_y = cluster_positions(horizontal_y, tolerance=4)
    vertical_x = cluster_positions(vertical_x, tolerance=4)

    spacings = [b - a for a, b in zip(horizontal_y, horizontal_y[1:]) if b - a > 4]
    row_variance = statistics.pvariance(spacings) if len(spacings) >= 2 else None
    dense_warning = bool(spacings and min(spacings) < 10)

    return len(horizontal_y), len(vertical_x), row_variance, dense_warning


def cluster_positions(positions: list[int], tolerance: int) -> list[int]:
    if not positions:
        return []

    positions = sorted(positions)
    clusters: list[list[int]] = [[positions[0]]]

    for position in positions[1:]:
        if position - clusters[-1][-1] <= tolerance:
            clusters[-1].append(position)
        else:
            clusters.append([position])

    return [round(statistics.mean(cluster)) for cluster in clusters]


def has_low_contrast_warning(image: Image.Image) -> bool:
    grayscale = image.convert("L")
    stat = ImageStat.Stat(grayscale)
    stddev = stat.stddev[0]
    mean = stat.mean[0]
    return stddev < 18 or mean < 24


def audit_image(path: Path) -> AuditResult:
    with Image.open(path) as image:
        image = image.convert("RGBA")
        width, height = image.size
        bounds = find_content_bounds(image)
        horizontal_count, vertical_count, row_variance, dense_warning = analyze_lines(image)
        low_contrast = has_low_contrast_warning(image)
        notes: list[str] = []

        edge_warning = False
        if bounds is None:
            notes.append("No visible content detected.")
        else:
            margins = [
                bounds.left,
                bounds.top,
                width - bounds.right - 1,
                height - bounds.bottom - 1,
            ]
            edge_warning = min(margins) < 6
            if edge_warning:
                notes.append("Visible content is close to at least one screenshot edge.")

        if cv2 is None or np is None:
            notes.append("OpenCV is unavailable; line and spacing diagnostics were skipped.")

        if row_variance is not None and row_variance > 400:
            notes.append("Detected row/section spacing varies significantly.")

        if dense_warning:
            notes.append("Detected tightly packed horizontal features; inspect for crowded controls.")

        if low_contrast:
            notes.append("Overall contrast appears low; inspect text and control readability.")

        return AuditResult(
            image=str(path),
            width=width,
            height=height,
            content_bounds=bounds,
            edge_margin_warning=edge_warning,
            horizontal_line_count=horizontal_count,
            vertical_line_count=vertical_count,
            row_spacing_variance=row_variance,
            low_contrast_warning=low_contrast,
            dense_region_warning=dense_warning,
            notes=notes,
        )


def write_reports(results: list[AuditResult], output_dir: Path) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)

    json_path = output_dir / "ui_audit_report.json"
    text_path = output_dir / "ui_audit_report.txt"

    json_path.write_text(
        json.dumps([asdict(result) for result in results], indent=2),
        encoding="utf-8",
    )

    lines: list[str] = []
    for result in results:
        lines.append(f"Image: {result.image}")
        lines.append(f"  Size: {result.width}x{result.height}")
        lines.append(f"  Content bounds: {result.content_bounds}")
        lines.append(f"  Horizontal lines: {result.horizontal_line_count}")
        lines.append(f"  Vertical lines: {result.vertical_line_count}")
        lines.append(f"  Row spacing variance: {result.row_spacing_variance}")
        lines.append(f"  Edge warning: {result.edge_margin_warning}")
        lines.append(f"  Low contrast warning: {result.low_contrast_warning}")
        lines.append(f"  Dense region warning: {result.dense_region_warning}")
        if result.notes:
            lines.append("  Notes:")
            lines.extend(f"    - {note}" for note in result.notes)
        lines.append("")

    text_path.write_text("\n".join(lines), encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Audit UI screenshots for visual presentation diagnostics.")
    parser.add_argument("input", type=Path, help="Screenshot file or directory of screenshots.")
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("artifacts/ui_audit"),
        help="Directory for generated JSON and text reports.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    images = list(iter_images(args.input))
    if not images:
        print(f"No supported images found at {args.input}")
        return 2

    results = [audit_image(path) for path in images]
    write_reports(results, args.output)

    warnings = sum(
        1
        for result in results
        if result.edge_margin_warning or result.low_contrast_warning or result.dense_region_warning
    )

    print(f"Audited {len(results)} image(s). Reports written to {args.output}.")
    print(f"Images with warnings: {warnings}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
