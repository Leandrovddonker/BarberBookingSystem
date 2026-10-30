import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import {
  Barber,
  HairCut,
  OpeningHours,
  User,
  RegisterDto,
  CreateBarberDto,
  HairCutDto,
  OpeningHoursDto,
  Reservation,
  BarberReservation,
  CreateReservationDto,
  UpdateReservationDto,
  LoginResponseDto,
  SpecialOpeningHours,
  CreateSpecialOpeningHoursDto,
  UpdateSpecialOpeningHoursDto,
} from './models';

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);
  private get<T>(path: string, params?: Record<string, string | number>) {
    return this.http.get<T>('/api/' + path, { params });
  }

  private post<T>(path: string, body: unknown) {
    return this.http.post<T>('/api/' + path, body);
  }

  private put<T>(path: string, body: unknown) {
    return this.http.put<T>('/api/' + path, body);
  }

  private delete(path: string) {
    return this.http.delete<void>('/api/' + path);
  }

  barbers() {
    return this.get<Barber[]>('Barber/all');
  }

  cuts() {
    return this.get<HairCut[]>('Cut/all');
  }

  hours() {
    return this.get<OpeningHours[]>('OpeningHours/all');
  }

  slots(
    barberId: number,
    haircutId: number,
    date: string,
    excludeReservationId?: number,
  ) {
    const params: Record<string, string | number> = {
      barberId,
      haircutId,
      date,
    };
    if (excludeReservationId !== undefined)
      params['excludeReservationId'] = excludeReservationId;
    return this.get<string[]>('Reservation/available-slots', params);
  }

  register(user: RegisterDto) {
    return this.post<User>('Auth/register', user);
  }

  login(email: string, password: string) {
    return this.post<LoginResponseDto>('Auth/login', { email, password });
  }

  users() {
    return this.get<User[]>('User/all');
  }

  reservations(isAdmin: boolean) {
    return this.get<Reservation[]>(
      isAdmin ? 'Reservation/all' : 'Reservation/me',
    );
  }

  barberSchedule() {
    return this.get<BarberReservation[]>('Reservation/barber/me');
  }

  createReservation(reservation: CreateReservationDto) {
    return this.post<Reservation>('Reservation', reservation);
  }

  updateReservation(id: number, update: UpdateReservationDto) {
    return this.put<Reservation>(`Reservation/${id}`, update);
  }

  cancelReservation(id: number) {
    return this.delete(`Reservation/${id}`);
  }

  createBarber(barber: CreateBarberDto) {
    return this.post<Barber>('Barber/RegisterBarber', barber);
  }

  createCut(cut: HairCutDto) {
    return this.post<HairCutDto>('Cut', cut);
  }

  updateCut(id: number, cut: HairCutDto) {
    return this.put<HairCutDto>(`Cut/${id}`, cut);
  }

  deleteCut(id: number) {
    return this.delete(`Cut/${id}`);
  }

  createHours(hours: OpeningHoursDto) {
    return this.post<OpeningHoursDto>('OpeningHours', hours);
  }

  updateHours(day: number, hours: OpeningHoursDto) {
    return this.put<OpeningHoursDto>(`OpeningHours/${day}`, hours);
  }

  deleteHours(id: number) {
    return this.delete(`OpeningHours/${id}`);
  }

  specialHours() {
    return this.get<SpecialOpeningHours[]>('SpecialOpeningHours');
  }

  specialHoursById(id: number) {
    return this.get<SpecialOpeningHours>(`SpecialOpeningHours/${id}`);
  }

  createSpecialHours(hours: CreateSpecialOpeningHoursDto) {
    return this.post<SpecialOpeningHours>('SpecialOpeningHours', hours);
  }

  updateSpecialHours(id: number, hours: UpdateSpecialOpeningHoursDto) {
    return this.put<SpecialOpeningHours>(`SpecialOpeningHours/${id}`, hours);
  }

  deleteSpecialHours(id: number) {
    return this.delete(`SpecialOpeningHours/${id}`);
  }

  specialHoursReservationCount(date: string) {
    return this.get<number>('SpecialOpeningHours/reservation-count', { date });
  }
}
