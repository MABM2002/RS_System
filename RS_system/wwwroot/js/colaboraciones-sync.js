/**
 * Sync Manager for Colaboraciones
 * Handles connection monitoring, offline queue, and synchronization
 */

const ColaboracionesSyncManager = {
    isOnline: navigator.onLine,
    isSyncing: false,
    syncInProgress: false,
    statusIndicator: null,
    pendingBadge: null,
    maxRetries: 3,
    retryDelay: 2000, // milliseconds
    
    /**
     * Initialize the sync manager
     */
    init() {
        console.log('[SyncManager] Initializing...');
        
        // Get UI elements
        this.statusIndicator = document.getElementById('offlineStatus');
        this.pendingBadge = document.getElementById('pendingBadge');
        
        // Listen for online/offline events
        window.addEventListener('online', () => this.handleOnline());
        window.addEventListener('offline', () => this.handleOffline());
        
        // Set initial status
        this.updateStatusUI();
        this.updatePendingBadge();
        
        // If online, try to sync pending items
        if (this.isOnline) {
            setTimeout(() => this.syncPending(), 1000);
        }
        
        console.log('[SyncManager] Initialized. Status:', this.isOnline ? 'Online' : 'Offline');
    },
    
    /**
     * Handle online event
     */
    async handleOnline() {
        console.log('[SyncManager] Connection restored');
        this.isOnline = true;
        this.updateStatusUI();
        
        // Auto-sync after short delay
        setTimeout(() => this.syncPending(), 500);
    },
    
    /**
     * Handle offline event
     */
    handleOffline() {
        console.log('[SyncManager] Connection lost');
        this.isOnline = false;
        this.updateStatusUI();
    },
    
    /**
     * Update status indicator UI
     */
    updateStatusUI() {
        if (!this.statusIndicator) return;
        
        if (this.isSyncing) {
            this.statusIndicator.className = 'badge bg-warning ms-2';
            this.statusIndicator.innerHTML = '<i class="bi bi-arrow-repeat"></i> Sincronizando';
            this.statusIndicator.style.display = 'inline-block';
        } else if (this.isOnline) {
            this.statusIndicator.className = 'badge bg-success ms-2';
            this.statusIndicator.innerHTML = '<i class="bi bi-wifi"></i> En línea';
            this.statusIndicator.style.display = 'inline-block';
        } else {
            this.statusIndicator.className = 'badge bg-secondary ms-2';
            this.statusIndicator.innerHTML = '<i class="bi bi-wifi-off"></i> Sin conexión';
            this.statusIndicator.style.display = 'inline-block';
        }
    },
    
    /**
     * Update pending items badge
     */
    async updatePendingBadge() {
        if (!this.pendingBadge) return;
        
        try {
            const count = await ColaboracionesOfflineDB.getPendingCount();
            
            if (count > 0) {
                this.pendingBadge.textContent = count;
                this.pendingBadge.style.display = 'inline-block';
            } else {
                this.pendingBadge.style.display = 'none';
            }
        } catch (error) {
            console.error('[SyncManager] Error updating badge:', error);
        }
    },
    
    /**
     * Save colaboracion (online or offline)
     */
    async saveColaboracion(colaboracionData) {
        if (this.isOnline) {
            try {
                // Try to save directly to server
                const result = await this.sendToServer(colaboracionData);
                
                if (result.success) {
                    return {
                        success: true,
                        message: 'Colaboración registrada exitosamente',
                        online: true
                    };
                } else {
                    throw new Error(result.message || 'Error al guardar');
                }
            } catch (error) {
                console.warn('[SyncManager] Online save failed, using offline mode:', error);
                // Fall back to offline save
                return await this.saveOffline(colaboracionData);
            }
        } else {
            // Save offline
            return await this.saveOffline(colaboracionData);
        }
    },
    
    /**
     * Save to offline queue
     */
    async saveOffline(colaboracionData) {
        try {
            const record = await ColaboracionesOfflineDB.addColaboracion(colaboracionData);
            await this.updatePendingBadge();
            
            return {
                success: true,
                offline: true,
                message: 'Guardado offline. Se sincronizará automáticamente cuando haya conexión.',
                id: record.id
            };
        } catch (error) {
            console.error('[SyncManager] Offline save failed:', error);
            return {
                success: false,
                message: 'Error al guardar en modo offline: ' + error.message
            };
        }
    },
    
    /**
     * Send colaboracion to server
     */
    async sendToServer(colaboracionData) {
        const formData = new FormData();
        
        formData.append('MiembroId', colaboracionData.miembroId);
        formData.append('MesInicial', colaboracionData.mesInicial);
        formData.append('AnioInicial', colaboracionData.anioInicial);
        formData.append('MesFinal', colaboracionData.mesFinal);
        formData.append('AnioFinal', colaboracionData.anioFinal);
        formData.append('MontoTotal', colaboracionData.montoTotal);
        formData.append('Observaciones', colaboracionData.observaciones || '');
        
        if (colaboracionData.tipoPrioritario) {
            formData.append('TipoPrioritario', colaboracionData.tipoPrioritario);
        }
        
        if (colaboracionData.tiposSeleccionados && colaboracionData.tiposSeleccionados.length > 0) {
            colaboracionData.tiposSeleccionados.forEach(tipo => {
                formData.append('TiposSeleccionados', tipo);
            });
        }
        
        const response = await fetch('/Colaboracion/Create', {
            method: 'POST',
            body: formData
        });
        
        if (response.redirected) {
            // Success - ASP.NET redirected to Index
            return { success: true };
        }
        
        const text = await response.text();
        
        // Check if response contains success indicators
        if (text.includes('exitosamente') || response.ok) {
            return { success: true };
        }
        
        throw new Error('Error en la respuesta del servidor');
    },
    
    /**
     * Synchronize all pending colaboraciones
     */
    async syncPending() {
        if (!this.isOnline || this.syncInProgress) {
            console.log('[SyncManager] Sync skipped. Online:', this.isOnline, 'InProgress:', this.syncInProgress);
            return;
        }
        
        try {
            this.syncInProgress = true;
            this.isSyncing = true;
            this.updateStatusUI();
            
            const pending = await ColaboracionesOfflineDB.getPending();
            
            if (pending.length === 0) {
                console.log('[SyncManager] No pending items to sync');
                return;
            }
            
            console.log(`[SyncManager] Syncing ${pending.length} pending item(s)...`);
            
            let successCount = 0;
            let failCount = 0;
            
            for (const item of pending) {
                try {
                    // Update status to syncing
                    await ColaboracionesOfflineDB.updateSyncStatus(item.id, 'syncing', item.retryCount);
                    
                    // Try to send to server
                    const result = await this.sendToServer(item);
                    
                    if (result.success) {
                        // Remove from offline DB
                        await ColaboracionesOfflineDB.remove(item.id);
                        successCount++;
                        console.log(`[SyncManager] Item ${item.id} synced successfully`);
                    } else {
                        throw new Error(result.message || 'Unknown error');
                    }
                } catch (error) {
                    console.error(`[SyncManager] Sync failed for item ${item.id}:`, error);
                    
                    // Update retry count
                    const newRetryCount = (item.retryCount || 0) + 1;
                    
                    if (newRetryCount >= this.maxRetries) {
                        await ColaboracionesOfflineDB.updateSyncStatus(item.id, 'failed', newRetryCount);
                        failCount++;
                    } else {
                        await ColaboracionesOfflineDB.updateSyncStatus(item.id, 'pending', newRetryCount);
                    }
                }
            }
            
            await this.updatePendingBadge();
            
            // Show results
            if (successCount > 0) {
                toastr.success(`${successCount} colaboración(es) sincronizada(s) exitosamente`);
                
                // Reload page to show updated data
                setTimeout(() => {
                    window.location.reload();
                }, 1500);
            }
            
            if (failCount > 0) {
                toastr.error(`${failCount} colaboración(es) no se pudieron sincronizar. Se reintentará automáticamente.`);
            }
            
        } catch (error) {
            console.error('[SyncManager] Sync error:', error);
            toastr.error('Error durante la sincronización');
        } finally {
            this.syncInProgress = false;
            this.isSyncing = false;
            this.updateStatusUI();
        }
    },
    
    /**
     * Manually trigger sync
     */
    async manualSync() {
        if (!this.isOnline) {
            toastr.warning('No hay conexión a internet');
            return;
        }
        
        toastr.info('Iniciando sincronización...');
        await this.syncPending();
    },
    
    /**
     * Check if currently online
     */
    checkOnlineStatus() {
        return this.isOnline;
    }
};

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        ColaboracionesSyncManager.init();
    });
} else {
    ColaboracionesSyncManager.init();
}
