/**
 * Offline Manager for Contabilidad
 * Handles connection monitoring, offline queue, and synchronization
 */

const OfflineManager = {
    isOnline: navigator.onLine,
    isSyncing: false,
    statusBadge: null,
    pendingCounter: null,
    syncInProgress: false,
    
    /**
     * Initialize the offline manager
     */
    init() {
        this.statusBadge = document.getElementById('connectionStatus');
        this.pendingCounter = document.getElementById('pendingCount');
        
        // Listen for online/offline events
        window.addEventListener('online', () => this.handleOnline());
        window.addEventListener('offline', () => this.handleOffline());
        
        // Initial status
        this.updateStatus();
        this.updatePendingCount();
        
        // Check for pending transactions on load
        if (this.isOnline) {
            this.syncPending();
        }
    },
    
    /**
     * Handle online event
     */
    async handleOnline() {
        this.isOnline = true;
        this.updateStatus();
        console.log('Connection restored - starting sync...');
        await this.syncPending();
    },
    
    /**
     * Handle offline event
     */
    handleOffline() {
        this.isOnline = false;
        this.updateStatus();
        console.log('Connection lost - offline mode enabled');
    },
    
    /**
     * Update connection status badge
     */
    updateStatus() {
        if (!this.statusBadge) return;
        
        if (this.isSyncing) {
            this.statusBadge.className = 'badge bg-warning';
            this.statusBadge.innerHTML = '<i class="fas fa-sync fa-spin"></i> Sincronizando';
        } else if (this.isOnline) {
            this.statusBadge.className = 'badge bg-success';
            this.statusBadge.innerHTML = '<i class="fas fa-wifi"></i> En línea';
        } else {
            this.statusBadge.className = 'badge bg-secondary';
            this.statusBadge.innerHTML = '<i class="fas fa-wifi-slash"></i> Sin conexión';
        }
    },
    
    /**
     * Update pending transaction counter
     */
    async updatePendingCount() {
        if (!this.pendingCounter) return;
        
        try {
            const count = await OfflineDB.getPendingCount();
            if (count > 0) {
                this.pendingCounter.textContent = `${count} pendiente${count > 1 ? 's' : ''}`;
                this.pendingCounter.style.display = 'inline-block';
            } else {
                this.pendingCounter.style.display = 'none';
            }
        } catch (error) {
            console.error('Error updating pending count:', error);
        }
    },
    
    /**
     * Save transaction (online or offline)
     */
    async saveTransaction(reporteId, movimientos, url) {
        if (this.isOnline) {
            // Try to save directly
            try {
                const response = await fetch(url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        ReporteId: reporteId,
                        Movimientos: movimientos
                    })
                });
                
                const result = await response.json();
                
                if (result.success) {
                    return { success: true, message: 'Guardado exitosamente', data: result };
                } else {
                    throw new Error(result.message || 'Error desconocido');
                }
            } catch (error) {
                // If online but request failed, save to queue
                console.warn('Request failed, saving to offline queue:', error);
                await this.saveOffline(reporteId, movimientos);
                return { 
                    success: true, 
                    offline: true, 
                    message: 'Guardado en cola offline. Se sincronizará automáticamente.' 
                };
            }
        } else {
            // Save to offline queue
            await this.saveOffline(reporteId, movimientos);
            return { 
                success: true, 
                offline: true, 
                message: 'Sin conexión. Guardado en cola offline.' 
            };
        }
    },
    
    /**
     * Save to offline queue
     */
    async saveOffline(reporteId, movimientos) {
        await OfflineDB.addPending(reporteId, movimientos);
        await this.updatePendingCount();
        console.log('Transaction saved to offline queue');
    },
    
    /**
     * Synchronize pending transactions
     */
    async syncPending() {
        if (!this.isOnline || this.syncInProgress) return;
        
        try {
            this.syncInProgress = true;
            this.isSyncing = true;
            this.updateStatus();
            
            const pending = await OfflineDB.getAllPending();
            
            if (pending.length === 0) {
                console.log('No pending transactions to sync');
                return;
            }
            
            console.log(`Syncing ${pending.length} pending transaction(s)...`);
            
            let successCount = 0;
            let failCount = 0;
            
            for (const transaction of pending) {
                try {
                    const response = await fetch('/ContabilidadGeneral/GuardarBulk', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            ReporteId: transaction.reporteId,
                            Movimientos: transaction.movimientos
                        })
                    });
                    
                    const result = await response.json();
                    
                    if (result.success) {
                        await OfflineDB.removePending(transaction.id);
                        successCount++;
                        console.log(`Transaction ${transaction.id} synced successfully`);
                    } else {
                        failCount++;
                        console.error(`Transaction ${transaction.id} failed:`, result.message);
                    }
                } catch (error) {
                    failCount++;
                    console.error(`Error syncing transaction ${transaction.id}:`, error);
                }
            }
            
            await this.updatePendingCount();
            
            if (successCount > 0) {
                alert(`Sincronización completa: ${successCount} registro(s) guardado(s).${failCount > 0 ? ` ${failCount} fallido(s).` : ''}`);
                location.reload(); // Refresh to show updated data
            } else if (failCount > 0) {
                alert(`Error en sincronización: ${failCount} registro(s) no se pudieron guardar.`);
            }
            
        } catch (error) {
            console.error('Sync error:', error);
        } finally {
            this.syncInProgress = false;
            this.isSyncing = false;
            this.updateStatus();
        }
    },
    
    /**
     * Manually trigger sync
     */
    async manualSync() {
        if (!this.isOnline) {
            alert('No hay conexión a internet. La sincronización se realizará automáticamente cuando se restaure la conexión.');
            return;
        }
        
        await this.syncPending();
    }
};

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => OfflineManager.init());
} else {
    OfflineManager.init();
}
