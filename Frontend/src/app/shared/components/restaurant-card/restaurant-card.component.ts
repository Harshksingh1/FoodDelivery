import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Restaurant } from '../../../core/models/restaurant.models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-restaurant-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './restaurant-card.component.html',
  styleUrl: './restaurant-card.component.scss'
})
export class RestaurantCardComponent {
  @Input({ required: true }) restaurant!: Restaurant;
  catalogUrl = environment.catalogUrl;
}
