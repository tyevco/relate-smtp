import DOMPurify from 'dompurify'

const SAFE_TAGS = [
  'p', 'br', 'b', 'i', 'u', 'strong', 'em', 'a', 'ul', 'ol', 'li', 'img',
  'table', 'tr', 'td', 'th', 'thead', 'tbody', 'tfoot', 'div', 'span',
  'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'blockquote', 'pre', 'code',
  'hr', 'sub', 'sup', 'small', 'mark', 'del', 'ins', 'address',
  'caption', 'col', 'colgroup', 'figure', 'figcaption',
]

const SAFE_ATTRS = [
  'href', 'src', 'alt', 'class', 'target', 'rel',
  'width', 'height', 'title', 'colspan', 'rowspan', 'scope',
]

/**
 * Sanitizes HTML content to prevent XSS attacks.
 * Only allows a whitelist of safe HTML tags and attributes.
 */
export function sanitizeHtml(html: string | null | undefined): string {
  if (!html) return ''

  return DOMPurify.sanitize(html, {
    ALLOWED_TAGS: SAFE_TAGS,
    ALLOWED_ATTR: SAFE_ATTRS,
    ALLOW_DATA_ATTR: false,
    ADD_ATTR: ['target'],
    FORBID_TAGS: ['script', 'style', 'iframe', 'object', 'embed', 'form', 'input'],
    FORBID_ATTR: ['onerror', 'onload', 'onclick', 'onmouseover', 'onfocus', 'onblur'],
  })
}
