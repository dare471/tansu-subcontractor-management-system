import type { AxiosResponse } from 'axios';

/** Имя файла из Content-Disposition (поддержка filename и filename*). */
export function parseContentDispositionFileName(disposition: string | undefined): string | undefined {
  if (!disposition) return undefined;

  const encoded = disposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (encoded?.[1]) {
    try {
      return decodeURIComponent(encoded[1].trim());
    } catch {
      return encoded[1].trim();
    }
  }

  const plain = disposition.match(/filename="?([^";]+)"?/i);
  return plain?.[1]?.trim();
}

/** Имя файла по типу ответа и запрошенному формату. */
export function resolveDownloadFileName(
  disposition: string | undefined,
  contentType: string | undefined,
  formatHint?: string
): string {
  const fromHeader = parseContentDispositionFileName(disposition);
  if (fromHeader) return fromHeader;

  if (contentType?.includes('application/pdf') || formatHint === 'pdf') {
    return 'report.pdf';
  }
  return 'report.csv';
}

export function triggerBlobDownload(res: AxiosResponse<Blob>, formatHint?: string): void {
  const disposition = res.headers['content-disposition'] as string | undefined;
  const contentType = res.headers['content-type'] as string | undefined;
  const fileName = resolveDownloadFileName(disposition, contentType, formatHint);

  const blob =
    res.data instanceof Blob && contentType && res.data.type !== contentType
      ? new Blob([res.data], { type: contentType })
      : res.data;

  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
}
