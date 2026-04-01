from __future__ import annotations

import html
import re
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(r"C:\Users\kerne\Downloads\rodi\new\MangoTaika")
DOCS = ROOT / "docs"
OUTPUT = DOCS / "livrables-officiels"
LOGO = ROOT / "wwwroot" / "images" / "logo.png"

@dataclass
class DocSpec:
    source: Path
    title: str
    subtitle: str
    file_stem: str
    section_number: int

DOCS_TO_BUILD = [
    DocSpec(
        source=DOCS / "schema-mcd-mld.md",
        title="Schema MCD et MLD complet",
        subtitle="Modele de donnees et architecture relationnelle du projet MangoTaika",
        file_stem="01_schema_mcd_mld_mangotaika_officiel",
        section_number=1,
    ),
    DocSpec(
        source=DOCS / "dictionnaire-donnees.md",
        title="Dictionnaire de donnees",
        subtitle="Description structuree des tables, champs et roles de donnees du projet MangoTaika",
        file_stem="02_dictionnaire_donnees_mangotaika_officiel",
        section_number=2,
    ),
    DocSpec(
        source=DOCS / "matrice-conformite-cahiers-charge.md",
        title="Matrice de conformite aux cahiers de charge",
        subtitle="Etat de conformite fonctionnelle et projet du systeme MangoTaika",
        file_stem="03_matrice_conformite_mangotaika_officiel",
        section_number=3,
    ),
]


def read_markdown(path: Path) -> str:
    return path.read_text(encoding="utf-8").lstrip("\ufeff")


def apply_inline_formatting(text: str) -> str:
    text = html.escape(text)
    text = re.sub(r"`([^`]+)`", r"<code>\1</code>", text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", text)
    text = re.sub(r"\*([^*]+)\*", r"<em>\1</em>", text)

    def repl_link(match: re.Match[str]) -> str:
        label = html.escape(match.group(1))
        target = html.escape(match.group(2))
        return f"<span class='md-link'>{label}</span> <span class='md-link-target'>({target})</span>"

    text = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", repl_link, text)
    return text


def parse_markdown(md_text: str, *, drop_first_h1: bool = False) -> str:
    lines = md_text.splitlines()
    out: list[str] = []
    paragraph: list[str] = []
    list_items: list[str] = []
    code_lines: list[str] = []
    code_lang: str | None = None
    table_lines: list[str] = []
    first_h1_skipped = False

    def flush_paragraph() -> None:
        nonlocal paragraph
        if paragraph:
            merged = " ".join(part.strip() for part in paragraph if part.strip())
            if merged:
                out.append(f"<p>{apply_inline_formatting(merged)}</p>")
            paragraph = []

    def flush_list() -> None:
        nonlocal list_items
        if list_items:
            out.append("<ul>")
            for item in list_items:
                out.append(f"<li>{apply_inline_formatting(item)}</li>")
            out.append("</ul>")
            list_items = []

    def flush_table() -> None:
        nonlocal table_lines
        if not table_lines:
            return
        rows = []
        for line in table_lines:
            stripped = line.strip()
            if not stripped:
                continue
            if re.fullmatch(r"\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)+\|?", stripped):
                continue
            cells = [cell.strip() for cell in stripped.strip('|').split('|')]
            rows.append(cells)
        if rows:
            out.append("<table class='doc-table'>")
            out.append("<thead><tr>" + "".join(f"<th>{apply_inline_formatting(cell)}</th>" for cell in rows[0]) + "</tr></thead>")
            if len(rows) > 1:
                out.append("<tbody>")
                for row in rows[1:]:
                    out.append("<tr>" + "".join(f"<td>{apply_inline_formatting(cell)}</td>" for cell in row) + "</tr>")
                out.append("</tbody>")
            out.append("</table>")
        table_lines = []

    def flush_code() -> None:
        nonlocal code_lines, code_lang
        if code_lines:
            title = "Diagramme Mermaid" if (code_lang or "").lower() == "mermaid" else "Bloc technique"
            out.append("<div class='code-block'>")
            out.append(f"<div class='code-title'>{html.escape(title)}</div>")
            out.append(f"<pre>{html.escape(chr(10).join(code_lines))}</pre>")
            out.append("</div>")
            code_lines = []
            code_lang = None

    for raw_line in lines:
        line = raw_line.rstrip("\n")
        stripped = line.strip()

        if code_lang is not None:
            if stripped.startswith("```"):
                flush_code()
            else:
                code_lines.append(raw_line)
            continue

        if stripped.startswith("```"):
            flush_paragraph()
            flush_list()
            flush_table()
            code_lang = stripped[3:].strip() or "text"
            code_lines = []
            continue

        if stripped.startswith("|"):
            flush_paragraph()
            flush_list()
            table_lines.append(stripped)
            continue
        else:
            flush_table()

        if not stripped:
            flush_paragraph()
            flush_list()
            continue

        heading_match = re.match(r"^(#{1,6})\s+(.*)$", stripped)
        if heading_match:
            flush_paragraph()
            flush_list()
            level = len(heading_match.group(1))
            if drop_first_h1 and level == 1 and not first_h1_skipped:
                first_h1_skipped = True
                continue
            text = apply_inline_formatting(heading_match.group(2).strip())
            out.append(f"<h{level}>{text}</h{level}>")
            continue

        list_match = re.match(r"^-\s+(.*)$", stripped)
        if list_match:
            flush_paragraph()
            list_items.append(list_match.group(1).strip())
            continue

        paragraph.append(stripped)

    flush_paragraph()
    flush_list()
    flush_table()
    flush_code()
    return "\n".join(out)


def base_styles() -> str:
    return """
    @page { size: A4; margin: 2cm 1.6cm 2cm 1.6cm; }
    body { font-family: 'Aptos', 'Segoe UI', Arial, sans-serif; color: #21313f; line-height: 1.45; font-size: 11pt; }
    .cover { page-break-after: always; min-height: 24cm; display: flex; flex-direction: column; justify-content: center; align-items: center; text-align: center; }
    .cover-logo { width: 96px; height: 96px; object-fit: contain; margin-bottom: 18px; }
    .cover-kicker { color: #5f7f43; font-weight: 700; letter-spacing: 0.08em; font-size: 11pt; text-transform: uppercase; margin-bottom: 8px; }
    .cover-title { font-size: 24pt; font-weight: 800; color: #25323d; margin: 0 0 12px 0; line-height: 1.2; }
    .cover-subtitle { max-width: 560px; font-size: 12pt; color: #5a6772; margin-bottom: 28px; }
    .cover-meta { font-size: 10.5pt; color: #6d7680; }
    .section-break { page-break-before: always; min-height: 8cm; display: flex; flex-direction: column; justify-content: center; }
    .section-kicker { color: #5f7f43; font-weight: 700; text-transform: uppercase; letter-spacing: 0.08em; font-size: 10pt; margin-bottom: 6px; }
    .section-title { font-size: 21pt; font-weight: 800; color: #25323d; margin: 0 0 8px 0; }
    .section-subtitle { color: #62707d; font-size: 11pt; max-width: 560px; }
    .toc-box { page-break-after: always; border: 1px solid #d5dee6; border-radius: 14px; padding: 20px 24px; background: #fbfcfd; }
    .toc-box h2 { margin-top: 0; }
    .toc-item { margin-bottom: 10px; }
    h1 { font-size: 20pt; color: #25323d; border-bottom: 3px solid #5f7f43; padding-bottom: 6px; margin-top: 0; }
    h2 { font-size: 15pt; color: #4f6e37; margin-top: 24px; margin-bottom: 10px; }
    h3 { font-size: 12.5pt; color: #31414f; margin-top: 18px; margin-bottom: 8px; }
    p { margin: 0 0 10px 0; }
    ul { margin: 0 0 12px 18px; padding-left: 0; }
    li { margin-bottom: 5px; }
    code { font-family: Consolas, 'Courier New', monospace; background: #f3f5f7; padding: 1px 4px; border-radius: 4px; color: #22313d; }
    .md-link { font-weight: 600; }
    .md-link-target { color: #687480; font-size: 9.5pt; }
    .doc-table { border-collapse: collapse; width: 100%; margin: 10px 0 14px 0; font-size: 9.5pt; }
    .doc-table th, .doc-table td { border: 1px solid #cad4de; padding: 7px 8px; vertical-align: top; }
    .doc-table th { background: #eef5e4; color: #31414f; font-weight: 700; }
    .doc-table tr:nth-child(even) td { background: #fafcfd; }
    .code-block { margin: 12px 0 16px 0; border: 1px solid #d3dbe3; border-radius: 10px; overflow: hidden; }
    .code-title { background: #2f3b46; color: white; font-weight: 700; padding: 8px 12px; font-size: 10pt; }
    pre { margin: 0; padding: 12px 14px; background: #f8fafc; font-family: Consolas, 'Courier New', monospace; font-size: 9pt; white-space: pre-wrap; word-break: break-word; }
    .source-box { margin-top: 18px; padding: 10px 12px; border-left: 4px solid #5f7f43; background: #f7faf3; color: #51606c; font-size: 10pt; }
    """


def render_cover(title: str, subtitle: str, meta: str) -> str:
    logo_html = f"<img src='{LOGO.as_uri()}' alt='Logo Mango Taika' class='cover-logo' />" if LOGO.exists() else ""
    return f"""
    <section class='cover'>
      {logo_html}
      <div class='cover-kicker'>Mango Taika District Scout</div>
      <h1 class='cover-title' style='border:none;padding:0;margin:0 0 12px 0'>{html.escape(title)}</h1>
      <div class='cover-subtitle'>{html.escape(subtitle)}</div>
      <div class='cover-meta'>{meta}</div>
    </section>
    """


def build_html(doc: DocSpec) -> str:
    body = parse_markdown(read_markdown(doc.source), drop_first_h1=True)
    cover = render_cover(doc.title, doc.subtitle, "Version de remise officielle<br/>Genere automatiquement depuis la documentation du depot")
    return f"""<!DOCTYPE html>
<html lang='fr'>
<head>
  <meta charset='utf-8' />
  <title>{html.escape(doc.title)} - MangoTaika</title>
  <style>{base_styles()}</style>
</head>
<body>
  {cover}
  <div class='source-box'>Source documentaire: <code>{html.escape(str(doc.source.relative_to(ROOT)).replace('\\', '/'))}</code></div>
  {body}
</body>
</html>
"""


def build_combined_html() -> str:
    cover = render_cover(
        "Dossier technique officiel MangoTaika",
        "Schema MCD/MLD, dictionnaire de donnees et matrice de conformite du projet",
        "Version consolidee de remise officielle<br/>Generee automatiquement depuis la documentation du depot"
    )
    toc_items = "".join(
        f"<div class='toc-item'><strong>{doc.section_number}. {html.escape(doc.title)}</strong><br/><span class='section-subtitle'>{html.escape(doc.subtitle)}</span></div>"
        for doc in DOCS_TO_BUILD
    )
    sections = []
    for doc in DOCS_TO_BUILD:
        sections.append(f"""
        <section class='section-break'>
          <div class='section-kicker'>Partie {doc.section_number}</div>
          <div class='section-title'>{html.escape(doc.title)}</div>
          <div class='section-subtitle'>{html.escape(doc.subtitle)}</div>
        </section>
        <div class='source-box'>Source documentaire: <code>{html.escape(str(doc.source.relative_to(ROOT)).replace('\\', '/'))}</code></div>
        {parse_markdown(read_markdown(doc.source), drop_first_h1=True)}
        """)
    body = "\n".join(sections)
    return f"""<!DOCTYPE html>
<html lang='fr'>
<head>
  <meta charset='utf-8' />
  <title>Dossier technique officiel MangoTaika</title>
  <style>{base_styles()}</style>
</head>
<body>
  {cover}
  <section class='toc-box'>
    <h2>Sommaire</h2>
    {toc_items}
  </section>
  {body}
</body>
</html>
"""


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for doc in DOCS_TO_BUILD:
        html_path = OUTPUT / f"{doc.file_stem}.html"
        html_path.write_text(build_html(doc), encoding="utf-8")
        print(f"Generated {html_path}")

    combined = OUTPUT / "00_dossier_technique_mangotaika_officiel.html"
    combined.write_text(build_combined_html(), encoding="utf-8")
    print(f"Generated {combined}")


if __name__ == "__main__":
    main()
