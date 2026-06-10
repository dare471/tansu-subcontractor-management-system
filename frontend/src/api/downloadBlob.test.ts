import { describe, expect, it } from 'vitest';
import { parseContentDispositionFileName, resolveDownloadFileName } from './downloadBlob';

describe('downloadBlob', () => {
  it('parses plain filename from Content-Disposition', () => {
    const header =
      "attachment; filename=journal-poseshcheniy-20260609-0156.pdf; filename*=UTF-8''journal-poseshcheniy-20260609-0156.pdf";
    expect(parseContentDispositionFileName(header)).toBe('journal-poseshcheniy-20260609-0156.pdf');
  });

  it('prefers filename* when present', () => {
    expect(parseContentDispositionFileName("attachment; filename*=UTF-8''report%2Epdf")).toBe('report.pdf');
  });

  it('falls back to pdf from content-type when header missing', () => {
    expect(resolveDownloadFileName(undefined, 'application/pdf', 'csv')).toBe('report.pdf');
  });

  it('falls back to pdf from format hint when header and type missing', () => {
    expect(resolveDownloadFileName(undefined, undefined, 'pdf')).toBe('report.pdf');
  });

  it('falls back to csv by default', () => {
    expect(resolveDownloadFileName(undefined, 'text/csv; charset=utf-8', 'csv')).toBe('report.csv');
  });
});
