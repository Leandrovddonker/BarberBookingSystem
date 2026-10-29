export type Role = 'Customer' | 'Barber' | 'Admin';
export interface RegisterDto {
  name: string;
  email: string;
  password: string;
  phoneNumber: string;
}
export interface Barber {
  id: number;
  name: string;
  email: string;
  phoneNumber: string;
  description: string | null;
  userId: number;
}
export interface HairCutDto {
  cut: string;
  price: number;
  durationMinutes: number;
}
export interface HairCut extends HairCutDto {
  id: number;
}
export interface OpeningHoursDto {
  day: number;
  openingTime: string;
  closingTime: string;
}
export interface OpeningHours extends OpeningHoursDto {
  id: number;
}
export interface Reservation {
  id: number;
  userId: number;
  barberId: number;
  hairCutId: number;
  startTime: string;
}
export interface BarberReservation {
  id: number;
  customerName: string;
  phoneNumber: string;
  haircutName: string;
  price: number;
  durationMinutes: number;
  startTime: string;
  endTime: string;
}
export interface User {
  id: number;
  name: string;
  email: string;
  phoneNumber: string;
  role: number;
  reservations: unknown[];
}
export interface LoginResponseDto {
  token: string;
}
export interface CreateReservationDto {
  barberId: number;
  hairCutId: number;
  startTime: string;
}
export interface UpdateReservationDto {
  startTime: string;
}
export interface CreateBarberDto extends RegisterDto {
  description: string;
}
