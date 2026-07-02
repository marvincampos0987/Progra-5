import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  IonContent,
  IonHeader,
  IonTitle,
  IonToolbar,
  IonGrid,
  IonRow,
  IonCol,
  IonSelect,
  IonSelectOption,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardContent,
  IonList,
  IonItem,
  IonLabel,
  IonAvatar,
  IonText,
  IonBadge,
  IonSpinner
} from '@ionic/angular/standalone';
import { ClasificationService } from '../../services/clasification.service';
import { ILeague } from '../../models/league.model';
import { ISeason } from '../../models/season.model';
import { IClasification } from '../../models/clasification.model';

@Component({
  selector: 'app-clasification-table',
  templateUrl: './clasification-table.page.html',
  styleUrls: ['./clasification-table.page.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IonContent,
    IonHeader,
    IonTitle,
    IonToolbar,
    IonGrid,
    IonRow,
    IonCol,
    IonSelect,
    IonSelectOption,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
    IonList,
    IonItem,
    IonLabel,
    IonAvatar,
    IonText,
    IonBadge,
    IonSpinner
  ]
})
export class ClasificationTablePage implements OnInit {
  leagues: ILeague[] = [];
  seasons: ISeason[] = [];
  tableData: IClasification[] = [];
  
  selectedLeagueId: string = '';
  selectedSeason: string = '';
  
  isLoading: boolean = false;
  errorMessage: string = '';

  constructor(private clasificationService: ClasificationService) {}

  ngOnInit() {
    this.loadLeagues();
  }

  loadLeagues() {
    this.isLoading = true;
    this.clasificationService.getFootballLeagues().subscribe({
      next: (data) => {
        this.leagues = data;
        if (this.leagues.length > 0) {
          // Select LaLiga (4335) or the first league by default
          const defaultLeague = this.leagues.find(l => l.idLeague === '4335') || this.leagues[0];
          this.selectedLeagueId = defaultLeague.idLeague;
          this.loadSeasons(this.selectedLeagueId);
        } else {
          this.isLoading = false;
        }
      },
      error: (err) => {
        this.errorMessage = 'Error loading leagues';
        this.isLoading = false;
      }
    });
  }

  onLeagueChange(event: any) {
    const leagueId = event.detail.value;
    if (leagueId) {
      this.selectedLeagueId = leagueId;
      this.loadSeasons(leagueId);
    }
  }

  loadSeasons(leagueId: string) {
    this.isLoading = true;
    this.clasificationService.getSeasons(leagueId).subscribe({
      next: (data) => {
        this.seasons = data;
        if (this.seasons.length > 0) {
          this.selectedSeason = this.seasons[0].strSeason; // Select latest season
          this.loadTable(this.selectedLeagueId, this.selectedSeason);
        } else {
          this.tableData = [];
          this.isLoading = false;
        }
      },
      error: (err) => {
        this.errorMessage = 'Error loading seasons';
        this.isLoading = false;
      }
    });
  }

  onSeasonChange(event: any) {
    const season = event.detail.value;
    if (season) {
      this.selectedSeason = season;
      this.loadTable(this.selectedLeagueId, this.selectedSeason);
    }
  }

  loadTable(leagueId: string, season: string) {
    this.isLoading = true;
    this.errorMessage = '';
    this.clasificationService.getTableClasification(leagueId, season).subscribe({
      next: (data) => {
        this.tableData = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = 'Error loading standings';
        this.isLoading = false;
      }
    });
  }

  getZoneClass(description: string): string {
    if (!description) return '';
    const desc = description.toLowerCase();
    if (desc.includes('champions league') || desc.includes('campeones') || desc.includes('campeón')) {
      return 'zone-champions';
    } else if (desc.includes('europa league')) {
      return 'zone-europa';
    } else if (desc.includes('relegation') || desc.includes('descenso')) {
      return 'zone-relegation';
    }
    return '';
  }

  getGoalDiff(diffString: string): number {
    return parseInt(diffString, 10) || 0;
  }

  getFormArray(formString: string): string[] {
    if (!formString) return [];
    return formString.split('');
  }
}
