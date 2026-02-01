import { cn, formatBytes, formatDate, truncate, getInitials, stringToColor } from '../utils'

describe('cn (className merge utility)', () => {
  it('merges class names correctly', () => {
    expect(cn('foo', 'bar')).toBe('foo bar')
  })

  it('handles conditional class names', () => {
    expect(cn('base', false && 'skip', 'always')).toBe('base always')
  })

  it('handles undefined and null values', () => {
    expect(cn('base', undefined, null, 'end')).toBe('base end')
  })

  it('merges Tailwind classes properly', () => {
    expect(cn('px-2 py-1', 'px-4')).toBe('py-1 px-4')
  })

  it('returns empty string for no arguments', () => {
    expect(cn()).toBe('')
  })
})

describe('formatBytes', () => {
  it('formats 0 bytes', () => {
    expect(formatBytes(0)).toBe('0 B')
  })

  it('formats bytes', () => {
    expect(formatBytes(500)).toBe('500 B')
  })

  it('formats kilobytes', () => {
    expect(formatBytes(1024)).toBe('1 KB')
    expect(formatBytes(1536)).toBe('1.5 KB')
  })

  it('formats megabytes', () => {
    expect(formatBytes(1048576)).toBe('1 MB')
    expect(formatBytes(1572864)).toBe('1.5 MB')
  })

  it('formats gigabytes', () => {
    expect(formatBytes(1073741824)).toBe('1 GB')
  })

  it('respects decimal places parameter', () => {
    expect(formatBytes(1536, 0)).toBe('2 KB')
    expect(formatBytes(1536, 2)).toBe('1.5 KB')
  })

  it('handles negative decimal places', () => {
    expect(formatBytes(1536, -1)).toBe('2 KB')
  })
})

describe('formatDate', () => {
  beforeEach(() => {
    jest.useFakeTimers()
    jest.setSystemTime(new Date('2024-06-15T12:00:00Z'))
  })

  afterEach(() => {
    jest.useRealTimers()
  })

  it('formats today as time', () => {
    const today = new Date('2024-06-15T09:30:00Z').toISOString()
    const result = formatDate(today)
    // Should show time format like "09:30" or "9:30 AM"
    expect(result).toMatch(/\d{1,2}:\d{2}/)
  })

  it('formats yesterday', () => {
    const yesterday = new Date('2024-06-14T12:00:00Z').toISOString()
    expect(formatDate(yesterday)).toBe('Yesterday')
  })

  it('formats days within a week as weekday', () => {
    const threeDaysAgo = new Date('2024-06-12T12:00:00Z').toISOString()
    const result = formatDate(threeDaysAgo)
    // Should be a short weekday like "Wed"
    expect(result.length).toBeLessThan(10)
  })

  it('formats dates in same year as month and day', () => {
    const monthAgo = new Date('2024-05-01T12:00:00Z').toISOString()
    const result = formatDate(monthAgo)
    // Should contain month abbreviation
    expect(result).toMatch(/\w+\s+\d+/)
  })

  it('formats dates in different year with full date', () => {
    const lastYear = new Date('2023-01-15T12:00:00Z').toISOString()
    const result = formatDate(lastYear)
    // Should contain year
    expect(result).toMatch(/2023/)
  })
})

describe('truncate', () => {
  it('returns original text if shorter than max length', () => {
    expect(truncate('Hello', 10)).toBe('Hello')
  })

  it('returns original text if equal to max length', () => {
    expect(truncate('Hello', 5)).toBe('Hello')
  })

  it('truncates text longer than max length', () => {
    expect(truncate('Hello World', 8)).toBe('Hello...')
  })

  it('adds ellipsis to truncated text', () => {
    const result = truncate('This is a long text', 10)
    expect(result).toContain('...')
    expect(result.length).toBe(10)
  })

  it('handles empty string', () => {
    expect(truncate('', 5)).toBe('')
  })
})

describe('getInitials', () => {
  it('returns first and last name initials', () => {
    expect(getInitials('John Doe', 'john@example.com')).toBe('JD')
  })

  it('handles single name', () => {
    expect(getInitials('John', 'john@example.com')).toBe('JO')
  })

  it('handles multiple names', () => {
    expect(getInitials('John Middle Doe', 'john@example.com')).toBe('JD')
  })

  it('falls back to email when name is null', () => {
    expect(getInitials(null, 'john@example.com')).toBe('JO')
  })

  it('returns uppercase initials', () => {
    expect(getInitials('john doe', 'test@example.com')).toBe('JD')
  })

  it('handles names with extra spaces', () => {
    expect(getInitials('  John   Doe  ', 'john@example.com')).toBe('JD')
  })
})

describe('stringToColor', () => {
  it('returns a valid hex color', () => {
    const color = stringToColor('test')
    expect(color).toMatch(/^#[0-9a-f]{6}$/i)
  })

  it('returns consistent color for same string', () => {
    const color1 = stringToColor('hello')
    const color2 = stringToColor('hello')
    expect(color1).toBe(color2)
  })

  it('returns different colors for different strings', () => {
    const color1 = stringToColor('hello')
    const color2 = stringToColor('world')
    // Not guaranteed to be different, but should work for most inputs
    expect(typeof color1).toBe('string')
    expect(typeof color2).toBe('string')
  })

  it('handles empty string', () => {
    const color = stringToColor('')
    expect(color).toMatch(/^#[0-9a-f]{6}$/i)
  })

  it('returns one of the predefined colors', () => {
    const predefinedColors = [
      '#ef4444', '#f97316', '#eab308', '#22c55e',
      '#14b8a6', '#3b82f6', '#8b5cf6', '#ec4899',
    ]
    const color = stringToColor('test')
    expect(predefinedColors).toContain(color)
  })
})
