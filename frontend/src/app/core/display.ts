import { HttpErrorResponse } from '@angular/common/http';
import type { OpeningHoursDto } from './models';

export const days = [
  'Sunday',
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
] as const;
const FALLBACK_ERROR = 'Something went wrong. Please try again.';

export function errorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return FALLBACK_ERROR;
  if (!error.status)
    return 'Unable to reach the shop. Check that the API is running, then try again.';
  if (error.status === 401)
    return 'Your session has expired. Please sign in again.';
  if (error.status === 403)
    return 'Your account does not have permission for this action.';

  const problem: unknown = error.error;
  if (!isRecord(problem)) return FALLBACK_ERROR;
  if (typeof problem['detail'] === 'string' && problem['detail'])
    return problem['detail'];
  const validationErrors = problem['errors'];
  if (isRecord(validationErrors)) {
    const message = Object.values(validationErrors)
      .flat()
      .filter((value) => typeof value === 'string')
      .join(' ');
    if (message) return message;
  }
  return typeof problem['title'] === 'string' && problem['title']
    ? problem['title']
    : FALLBACK_ERROR;
}

export function openingHoursLabel(hours: OpeningHoursDto): string {
  return hours.openingTime >= hours.closingTime
    ? 'Closed'
    : `${hours.openingTime.slice(0, 5)} – ${hours.closingTime.slice(0, 5)}`;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}
