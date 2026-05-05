from __future__ import annotations

from .models import BeamData


SECTION_FAMILY_COLORS: dict[str, str] = {
    "Tapered I": "#0f766e",
    "Wide Flange": "#2563eb",
    "Channel": "#dc2626",
    "Angle": "#d97706",
    "Double Angle": "#b45309",
    "Tee": "#7c3aed",
    "Tube": "#0891b2",
    "Pipe": "#4f46e5",
    "Tapered Tube": "#0ea5e9",
    "Circle": "#16a34a",
    "Rect": "#475569",
    "Trapezoid": "#be123c",
    "Box": "#1f77b4",
}


def beam_section_family(beam: BeamData | None) -> str:
    if beam is None:
        return "Box"

    prop_type = beam.section_property_type
    if prop_type == 680:
        return "Tapered I"
    if prop_type in {690, 698}:
        return "Wide Flange"
    if prop_type == 691:
        return "Channel"
    if prop_type == 692:
        return "Angle"
    if prop_type == 693:
        return "Double Angle"
    if prop_type in {673, 694}:
        return "Tee"
    if prop_type == 696:
        return "Tube"
    if prop_type == 695:
        return "Pipe"
    if prop_type == 675:
        return "Tapered Tube"
    if prop_type == 671:
        return "Circle"
    if prop_type in {672, 699}:
        return "Rect"
    if prop_type == 674:
        return "Trapezoid"

    label = f"{beam.section_display_name or ''} {beam.section_name or ''}".upper()
    if "TAPER" in label and "TUBE" in label:
        return "Tapered Tube"
    if "TAPER" in label:
        return "Tapered I"
    if "PIPE" in label:
        return "Pipe"
    if "TUBE" in label or "HSS" in label:
        return "Tube"
    if "CHANNEL" in label or label.startswith("C "):
        return "Channel"
    if "ANGLE" in label or label.startswith("L "):
        return "Angle"
    if "TEE" in label:
        return "Tee"
    if "RECT" in label or "BOX" in label:
        return "Rect"
    if "TRAP" in label:
        return "Trapezoid"
    if "CIRCLE" in label or "ROUND" in label:
        return "Circle"
    return "Wide Flange" if _looks_like_i_section(beam) else "Box"


def beam_section_type_text(beam: BeamData | None) -> str:
    if beam is None:
        return "-"
    prop_type = "-" if beam.section_property_type is None else str(beam.section_property_type)
    return f"{beam_section_family(beam)} ({prop_type})"


def beam_section_name_text(beam: BeamData | None) -> str:
    if beam is None:
        return "-"
    return beam.section_display_name or beam.section_name or beam_section_family(beam)


def beam_section_color(beam: BeamData | None) -> str:
    return SECTION_FAMILY_COLORS.get(beam_section_family(beam), SECTION_FAMILY_COLORS["Box"])


def _looks_like_i_section(beam: BeamData) -> bool:
    if beam.web_thickness is None or beam.web_thickness <= 0.0:
        return False
    flange_top = beam.flange_thickness_top or beam.flange_thickness_bottom
    if flange_top is None or flange_top <= 0.0:
        return False

    label = f"{beam.section_display_name or ''} {beam.section_name or ''}".upper()
    excluded_tokens = ("PIPE", "TUBE", "ANGLE", "CHANNEL", "TEE", "RECT", "ROUND", "TRAP")
    if any(token in label for token in excluded_tokens):
        return False
    return True
